using devfornet.db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Renci.SshNet;
using Spectre.Console;

namespace devfornet.cms
{
    enum CmsCommand
    {
        PushArticle = 11,
        PushCommunity = 12,
        DeleteArticle = 21,
        DeleteCommunity = 22,
        Exit = 99,
    }

    /// <summary>
    /// Simple command line CMS for devfornet. Pushes articles, communities and other content to the database.
    /// </summary>
    internal class Program
    {
        private static readonly IConfiguration Configuration;
        private const string RemoteResourcesFolder = "_resources";
        private const string RemoteArticlesFolder = "articles";

        private static readonly string _dbHost;
        private static readonly string _dbUser;
        private static readonly string _dbPassword;

        private static readonly string _wwwroot;

        private static readonly string _sshHost;
        private static readonly string _sshUser;
        private static readonly string _sshPassword;

        static Program()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();

            Configuration = configBuilder.Build();

            _dbHost = GetSetting("Postgres:Host", "POSTGRES_HOST");
            _dbUser = GetSetting("Postgres:User", "POSTGRES_USER");
            _dbPassword = GetSetting("Postgres:Password", "POSTGRES_PASSWORD");

            _wwwroot = GetSetting("WWWROOT", "WWWROOT", "./wwwroot");

            _sshHost = GetSetting("SSH:Host", "SSH_HOST");
            _sshUser = GetSetting("SSH:User", "SSH_USER");
            _sshPassword = GetSetting("SSH:Password", "SSH_PASSWORD");
        }

        static async Task Main(string[] args)
        {
            string connectionString =
                $"Host={_dbHost};Username={_dbUser};Password={_dbPassword};Database=devfornet";

            using DevForNetDbContext db = new DevForNetDbContext(
                new DbContextOptionsBuilder<DevForNetDbContext>()
                    .UseNpgsql(connectionString)
                    .Options
            );

            AnsiConsole.MarkupLine("devfor.net CMS CLI");

            CmsCommand cmd;
            do
            {
                AnsiConsole.MarkupLine(
                    @"Commands:
[bold][underline green]Push[/][/]
11. Article
12. Community
[bold][underline red]Delete[/][/]
21. Article
22. Community

[bold][red]99. Exit[/][/]"
                );

                cmd = (CmsCommand)
                    await AnsiConsole.PromptAsync(
                        new TextPrompt<int>("Command")
                            .AddChoices(Enum.GetValues<CmsCommand>().Select(t => (int)t))
                            .ShowChoices(false)
                            .DefaultValue(11)
                    );
                switch (cmd)
                {
                    case CmsCommand.PushArticle:
                        AnsiConsole.MarkupLine("");
                        AnsiConsole.MarkupLine("[bold][green]New Article[/][/]");
                        string title = await AnsiConsole.PromptAsync(
                            new TextPrompt<string>("Title")
                        );
                        string article = await AnsiConsole.PromptAsync(
                            new TextPrompt<string>("Article")
                        );
                        List<string> tags = (
                            await AnsiConsole.PromptAsync(
                                new TextPrompt<string>("Tags (comma separated)")
                            )
                        )
                            .Split(',')
                            .ToList();
                        string folder = await AnsiConsole.PromptAsync(
                            new TextPrompt<string>("Content Folder").DefaultValue("articles")
                        );
                        await PushArticle(db, title, article, folder, tags);
                        break;
                }
            } while (cmd != CmsCommand.Exit);
        }

        private static async Task PushArticle(
            DevForNetDbContext context,
            string title,
            string article,
            string folder,
            List<string>? tags = default
        )
        {
            // Search for .md file recursively in the folder directory
            string? mdFilePath = Directory
                .EnumerateFiles(folder, "*.md", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (mdFilePath is not null)
            {
                await CopyFileToRemote(
                    mdFilePath,
                    _sshHost,
                    _sshUser,
                    _sshPassword,
                    Path.Combine(_wwwroot, RemoteArticlesFolder)
                );
            }

            // If _resources folder exists, copy all its contents to _wwwroot
            string resourcesDir = Path.Combine(folder, "_resources");
            if (Directory.Exists(resourcesDir))
            {
                foreach (
                    string resourceFile in Directory.EnumerateFiles(
                        resourcesDir,
                        "*",
                        SearchOption.AllDirectories
                    )
                )
                {
                    await CopyFileToRemote(
                        resourceFile,
                        _sshHost,
                        _sshUser,
                        _sshPassword,
                        Path.Combine(_wwwroot, RemoteResourcesFolder)
                    );
                }
            }

            await context.DevForNetArticles.AddAsync(
                new Shared.Models.DevForNetArticle()
                {
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Title = title,
                    MarkdownUri = article,
                    Tags = tags ?? new List<string>(),
                }
            );
        }

        private static async Task CopyFileToRemote(
            string localFilePath,
            string remoteHost,
            string remoteUsername,
            string remotePassword,
            string remoteDirectory,
            CancellationToken cancellationToken = default
        )
        {
            using var client = new ScpClient(remoteHost, remoteUsername, remotePassword);
            await client.ConnectAsync(cancellationToken);
            using var fileStream = File.OpenRead(localFilePath);
            string remoteFilePath = Path.Combine(remoteDirectory, Path.GetFileName(localFilePath))
                .Replace("\\", "/");
            client.Upload(fileStream, remoteFilePath);
            client.Disconnect();
        }

        private static string GetSetting(
            string configKey,
            string envKey,
            string? defaultValue = null
        )
        {
            var v = Configuration[configKey];
            if (!string.IsNullOrEmpty(v))
                return v;
            v = Configuration[envKey]; // env vars may be present as their raw names
            if (!string.IsNullOrEmpty(v))
                return v;
            v = Environment.GetEnvironmentVariable(envKey);
            if (!string.IsNullOrEmpty(v))
                return v;
            if (defaultValue is not null)
                return defaultValue;

            throw new ArgumentException(
                $"Configuration value for '{configKey}' or '{envKey}' not defined."
            );
        }
    }
}

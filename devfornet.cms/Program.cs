using System.Diagnostics;
using System.Formats.Tar;
using devfornet.db;
using Docker.DotNet;
using Docker.DotNet.Models;
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
        private const string RemoteArticlesFolder = "Articles";

        private static readonly string _dbHost;
        private static readonly string _dbUser;
        private static readonly string _dbPassword;

        private static readonly string _wwwroot;

        private static readonly string _remoteHost;
        private static readonly string _remoteUser;
        private static readonly string _remotePassword;

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

            _remoteHost = GetSetting("Remote:Host", "REMOTE_HOST");
            _remoteUser = GetSetting("Remote:User", "REMOTE_USER");
            _remotePassword = GetSetting("Remote:Password", "REMOTE_PASSWORD");
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
                    _remoteHost,
                    _remoteUser,
                    _remotePassword,
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
                        _remoteHost,
                        _remoteUser,
                        _remotePassword,
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
                    ContentType = Shared.Models.ContentType.DevForNetArticle,
                    PublishedDate = DateTime.UtcNow,
                }
            );

            await context.SaveChangesAsync();
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
            if (
                !string.IsNullOrEmpty(remoteHost)
                && remoteHost.StartsWith("docker:", StringComparison.OrdinalIgnoreCase)
            )
            {
                string containerName = remoteHost.Substring("docker:".Length);
                if (string.IsNullOrWhiteSpace(containerName))
                    throw new ArgumentException(
                        "Docker container name not specified in remoteHost (use 'docker:<container>')."
                    );

                await CopyFileToDocker(containerName, localFilePath, remoteDirectory);
            }
            else
            {
                using var client = new ScpClient(remoteHost, remoteUsername, remotePassword);
                await client.ConnectAsync(cancellationToken);
                using var fileStream = File.OpenRead(localFilePath);
                string remoteFilePath = Path.Combine(
                        remoteDirectory,
                        Path.GetFileName(localFilePath)
                    )
                    .Replace("\\", "/");
                client.Upload(fileStream, remoteFilePath);
                client.Disconnect();
            }
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

        public static async Task CopyFileToDocker(
            string containerNameOrId,
            string localFilePath,
            string containerPath, // e.g. "/app/wwwroot/articles"
            CancellationToken ct = default
        )
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException(localFilePath);

            var dockerUri =
                Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? new Uri("npipe://./pipe/docker_engine")
                    : new Uri("unix:///var/run/docker.sock");

            using var docker = new DockerClientConfiguration(dockerUri).CreateClient();

            var containers = await docker.Containers.ListContainersAsync(
                new ContainersListParameters { All = true },
                ct
            );
            var container = containers.FirstOrDefault(c =>
                c.ID == containerNameOrId
                || c.Names.Any(n =>
                    n.TrimStart('/').Equals(containerNameOrId, StringComparison.OrdinalIgnoreCase)
                )
            );
            if (container == null)
                throw new InvalidOperationException($"Container '{containerNameOrId}' not found.");

            using var mem = new MemoryStream();
            using (var tarWriter = new TarWriter(mem, TarEntryFormat.Ustar, true))
            {
                string entryName = Path.GetFileName(localFilePath);
                await tarWriter.WriteEntryAsync(localFilePath, entryName);
            }
            mem.Seek(0, SeekOrigin.Begin);

            var extractParams = new ContainerPathStatParameters { Path = containerPath };
            await docker.Containers.ExtractArchiveToContainerAsync(
                container.ID,
                extractParams,
                mem,
                ct
            );
        }
    }
}

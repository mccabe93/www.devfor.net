using devfornet.db;
using devfornet.Shared.Models;
using github.api.net;
using github.api.net.Models.Search;
using Microsoft.EntityFrameworkCore;

namespace devfornet.repos
{
    internal class Program
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static SemaphoreSlim _dbMutex = new SemaphoreSlim(1, 1);

        static async Task Main(string[] args)
        {
            string postgresHost =
                Environment.GetEnvironmentVariable("POSTGRES_HOST")
                ?? throw new ArgumentException("Postgres Host not defined.");
            string postgresUser =
                Environment.GetEnvironmentVariable("POSTGRES_USER")
                ?? throw new ArgumentException("Postgres User not defined.");
            string postgresPassword =
                Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                ?? throw new ArgumentException("Postgres Password not defined.");
            string githubToken =
                Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                ?? throw new ArgumentException("GitHub Token not defined.");

            string connectionString =
                $"Host={postgresHost};Database=devfornet;Username={postgresUser};Password={postgresPassword};Persist Security Info=True";
            var optionsBuilder = new DbContextOptionsBuilder<DevForNetDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            using var context = new DevForNetDbContext(optionsBuilder.Options);
            using var githubClient = new GitHubClient(
                new github.api.net.Configuration.GitHubClientOptions()
                {
                    Token = githubToken,
                    UserAgent = "devfor.net",
                    BaseUrl = "https://api.github.com/",
                    MaxRetries = 1,
                    Timeout = TimeSpan.FromSeconds(30),
                }
            );
            Task[] tasks = new Task[4]
            {
                Task.Run(
                    async () => await MonitorHelpNeededNoob(context, githubClient),
                    _cts.Token
                ),
                Task.Run(async () => await MonitorHelpNeededAll(context, githubClient), _cts.Token),
                Task.Run(async () => await MonitorLatestCreated(context, githubClient), _cts.Token),
                Task.Run(async () => await MonitorLatestUpdated(context, githubClient), _cts.Token),
            };
            await Task.WhenAll(tasks);
        }

        ~Program()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        private static async Task MonitorHelpNeededNoob(
            DevForNetDbContext database,
            GitHubClient client
        )
        {
            DotnetRepo baseRecord = new DotnetRepo()
            {
                ContentGuid = string.Empty,
                Description = string.Empty,
                Name = string.Empty,
                Url = string.Empty,
                HelpWanted = true,
                NoobFriendly = true,
                Tags = new List<string>(),
            };
            while (!_cts.IsCancellationRequested)
            {
                await _dbMutex.WaitAsync();
                try
                {
                    Console.WriteLine("Checking for noob-friendly repositories needing help...");
                    string query = $"NET good-first-issues:>0 archived:false language:C# is:public";
                    var repos = await GetSearchResults(client, query, 0);
                    if (repos == null)
                    {
                        continue;
                    }
                    await SaveRecordsToDatabase(repos, database, baseRecord);
                    Console.WriteLine(
                        "Completed checking for noob-friendly repositories needing help."
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring feed: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
                }
                finally
                {
                    _dbMutex.Release();
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
                }
            }
        }

        private static async Task MonitorHelpNeededAll(
            DevForNetDbContext database,
            GitHubClient client
        )
        {
            DotnetRepo baseRecord = new DotnetRepo()
            {
                ContentGuid = string.Empty,
                Description = string.Empty,
                Name = string.Empty,
                Url = string.Empty,
                HelpWanted = true,
                NoobFriendly = false,
                Tags = new List<string>(),
            };
            while (!_cts.IsCancellationRequested)
            {
                await _dbMutex.WaitAsync();
                try
                {
                    Console.WriteLine("Checking for repositories needing help...");
                    string query =
                        $"NET help-wanted-issues:>0 archived:false language:C# is:public";
                    var repos = await GetSearchResults(client, query, 0);
                    if (repos == null)
                    {
                        continue;
                    }
                    await SaveRecordsToDatabase(repos, database, baseRecord);
                    Console.WriteLine("Completed checking for repositories needing help.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring feed: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
                }
                finally
                {
                    _dbMutex.Release();
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
                }
            }
        }

        private static async Task MonitorLatestUpdated(
            DevForNetDbContext database,
            GitHubClient client
        )
        {
            while (!_cts.IsCancellationRequested)
            {
                await _dbMutex.WaitAsync();
                try
                {
                    Console.WriteLine("Checking for recently updated repositories...");
                    string lastDay = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    string query =
                        $"NET published:>={lastDay} archived:false language:C# is:public";
                    var repos = await GetSearchResults(client, query, 0);
                    if (repos == null)
                    {
                        continue;
                    }
                    await SaveRecordsToDatabase(repos, database);
                    Console.WriteLine("Completed checking for recently updated repositories.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring feed: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
                }
                finally
                {
                    _dbMutex.Release();
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
                }
            }
        }

        private static async Task MonitorLatestCreated(
            DevForNetDbContext database,
            GitHubClient client
        )
        {
            while (!_cts.IsCancellationRequested)
            {
                await _dbMutex.WaitAsync();
                try
                {
                    Console.WriteLine("Checking for newly created repositories...");
                    string lastDay = DateTime.UtcNow.ToString("yyyy-MM-dd");
                    string query = $"NET created:>={lastDay} archived:false language:C# is:public";
                    var repos = await GetSearchResults(client, query, 0);
                    if (repos == null)
                    {
                        continue;
                    }
                    await SaveRecordsToDatabase(repos, database);
                    Console.WriteLine("Completed checking for newly created repositories.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring feed: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), _cts.Token);
                }
                finally
                {
                    _dbMutex.Release();
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
                }
            }
        }

        private static async Task<RepositorySearchResult?> GetSearchResults(
            GitHubClient client,
            string query,
            int page
        )
        {
            var result = await client.Search.SearchRepositoriesAsync(
                query,
                page: page,
                cancellationToken: _cts.Token
            );
            return result.Data;
        }

        private static async Task SaveRecordsToDatabase(
            RepositorySearchResult records,
            DevForNetDbContext database,
            DotnetRepo? baseRecord = null
        )
        {
            Console.WriteLine($"Found {records.TotalCount} repositories for current query.");
            foreach (var repo in records.Items)
            {
                DotnetRepo d4netRepo = new DotnetRepo()
                {
                    Publisher = repo.Owner.Login,
                    ContentType = ContentType.Repo,
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Name = repo.Name,
                    Description = repo.Description ?? "No description available.",
                    Url = repo.HtmlUrl,
                    PublishedDate = new DateTimeOffset(repo.CreatedAt.DateTime).UtcDateTime,
                    Score = repo.StargazersCount,
                    ManagedByDevForNet = false,
                    LastUpdated = new DateTimeOffset(repo.UpdatedAt.DateTime).UtcDateTime,
                    ForkCount = repo.ForksCount,
                    WatchersCount = repo.WatchersCount,
                    IssuesCount = repo.OpenIssuesCount,
                    Tags = repo.Topics ?? new List<string>(),
                };
                if (baseRecord != null)
                {
                    d4netRepo.HelpWanted = baseRecord.HelpWanted;
                    d4netRepo.NoobFriendly = baseRecord.NoobFriendly;
                }
                var existingRecord = await database.Repos.FirstOrDefaultAsync(
                    t => t.Url == d4netRepo.Url,
                    _cts.Token
                );
                if (existingRecord != null)
                {
                    existingRecord.Description = d4netRepo.Description;
                    existingRecord.ForkCount = d4netRepo.ForkCount;
                    existingRecord.IssuesCount = d4netRepo.IssuesCount;
                    existingRecord.LastUpdated = d4netRepo.LastUpdated;
                    existingRecord.NoobFriendly = d4netRepo.NoobFriendly;
                    existingRecord.Score = d4netRepo.Score;
                    existingRecord.HelpWanted = d4netRepo.HelpWanted;
                    existingRecord.Tags = d4netRepo.Tags;
                    existingRecord.WatchersCount = d4netRepo.WatchersCount;
                    continue;
                }
                while (
                    await database.ContentRecords.AnyAsync(
                        r => r.ContentGuid == d4netRepo.ContentGuid,
                        _cts.Token
                    )
                )
                {
                    d4netRepo.ContentGuid = Guid.CreateVersion7().ToString();
                }
                await database.ContentRecords.AddAsync(
                    new ContentRecord()
                    {
                        ContentGuid = d4netRepo.ContentGuid,
                        ContentType = ContentType.Repo,
                        Score = d4netRepo.Score,
                        PublishedDate = d4netRepo.PublishedDate,
                    },
                    _cts.Token
                );
                await database.Repos.AddAsync(d4netRepo);
            }
            await database.SaveChangesAsync(_cts.Token);
        }
    }
}

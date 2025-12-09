using System.ServiceModel.Syndication;
using System.Xml;
using devfornet.db;
using devfornet.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace devfornet.rss
{
    /// <summary>
    /// Simple RSS server. Monitors RSS feeds retrieved by a database and adds the articles to the database.
    /// </summary>
    internal class Program
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static SemaphoreSlim _dbMutex = new SemaphoreSlim(1, 1);
        private static int _maxArticlesPerFeed = 50;

        static async Task Main(string[] args)
        {
            if (args.Length > 0 && int.TryParse(args[0], out int maxArticles))
            {
                _maxArticlesPerFeed = maxArticles;
            }
            string postgresHost =
                Environment.GetEnvironmentVariable("POSTGRES_HOST")
                ?? throw new ArgumentException("Postgres Host not defined.");
            string postgresUser =
                Environment.GetEnvironmentVariable("POSTGRES_USER")
                ?? throw new ArgumentException("Postgres User not defined.");
            string postgresPassword =
                Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                ?? throw new ArgumentException("Postgres Password not defined.");

            string connectionString =
                $"Host={postgresHost};Database=devfornet;Username={postgresUser};Password={postgresPassword};Persist Security Info=True";
            var optionsBuilder = new DbContextOptionsBuilder<DevForNetDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            using var context = new DevForNetDbContext(optionsBuilder.Options);
            var rssFeeds = await context.RssFeeds.ToListAsync();
            await Parallel.ForEachAsync(
                rssFeeds,
                async (feed, cancelToken) =>
                {
                    await MonitorFeed(context, feed);
                }
            );
        }

        ~Program()
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        private static async Task MonitorFeed(DevForNetDbContext database, RssFeed feed)
        {
            while (!_cts.IsCancellationRequested)
            {
                await _dbMutex.WaitAsync();
                try
                {
                    Console.WriteLine($"Processing feed {feed.Name}");
                    int articlesProcessed = 0;
                    using (var reader = XmlReader.Create(feed.Url))
                    {
                        SyndicationFeed rss = SyndicationFeed.Load(reader);
                        Console.WriteLine($"Found {rss.Items.Count()} articles.");
                        foreach (var item in rss.Items)
                        {
                            RssArticle article = new RssArticle()
                            {
                                ContentGuid = Guid.CreateVersion7().ToString(),
                                Title = item.Title.Text,
                                ContentType = ContentType.RssArticle,
                                Publisher = feed.Name,
                                PublishedDate = item.PublishDate.UtcDateTime,
                                Url = item.Links[0].Uri.ToString(),
                                Tags = feed.Tags,
                                Score = feed.BaseScore,
                            };
                            if (article.PublishedDate == DateTime.MinValue)
                                article.PublishedDate = DateTime.UtcNow;
                            if (await database.RssArticles.AnyAsync(a => a.Url == article.Url))
                            {
                                continue;
                            }
                            ContentRecord record = new ContentRecord()
                            {
                                ContentGuid = article.ContentGuid,
                                ContentType = article.ContentType,
                                PublishedDate = article.PublishedDate,
                                Score = feed.BaseScore,
                            };
                            database.ContentRecords.Add(record);
                            database.RssArticles.Add(article);
                            Console.WriteLine($"Added {article.Title} ({article.ContentGuid})");
                            if (articlesProcessed++ >= _maxArticlesPerFeed)
                            {
                                break;
                            }
                        }
                    }
                    if (articlesProcessed > 0)
                    {
                        Console.WriteLine(
                            $"Saving {articlesProcessed} articles for feed {feed.Name}"
                        );
                        await database.SaveChangesAsync(_cts.Token);
                    }
                    else
                    {
                        Console.WriteLine($"No new articles for feed {feed.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error monitoring feed {feed.Name}: {ex.Message}");
                }
                finally
                {
                    _dbMutex.Release();
                    await Task.Delay(TimeSpan.FromMinutes(10), _cts.Token);
                }
            }
        }
    }
}

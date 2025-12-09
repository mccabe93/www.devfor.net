using devfornet.ApiService.Repos;
using devfornet.db;
using devfornet.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

namespace devfornet.ApiService
{
    internal static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration["Postgres:Host"] ??=
                Environment.GetEnvironmentVariable("POSTGRES_HOST")
                ?? throw new ArgumentException("Postgres Host not defined.");
            builder.Configuration["Postgres:User"] ??=
                Environment.GetEnvironmentVariable("POSTGRES_USER")
                ?? throw new ArgumentException("Postgres User not defined.");
            builder.Configuration["Postgres:Password"] ??=
                Environment.GetEnvironmentVariable("POSTGRES_PASSWORD")
                ?? throw new ArgumentException("Postgres Password not defined.");

            string usersConnectionString =
                $"Host={builder.Configuration["Postgres:Host"]};Database=devfornet;Username={builder.Configuration["Postgres:User"]};Password={builder.Configuration["Postgres:Password"]};Persist Security Info=True";

            builder.Services.AddDbContext<DevForNetDbContext>(options =>
                options.UseNpgsql(usersConnectionString)
            );
            builder.Services.AddMemoryCache();
            builder.Services.AddLogging();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            builder.Services.AddSingleton<ContentRepo>();
            builder.Services.AddSingleton<HistoryRepo>();

            // Add service defaults & Aspire client integrations.
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddProblemDetails();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "devfor.net API",
                        Version = "v1",
                        Description = "API for devfor.net platform",
                    }
                );
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
                if (await db.Database.EnsureCreatedAsync())
                {
                    await PopulateDefaultRssFeeds(db);
                    await PopulateDefaultCommunities(db);
                    await PopulateDevForNetArticles(db);
                }
            }

            app.UseCors();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "devfor.net API v1");
                    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                });
            }
            // Configure the HTTP request pipeline.
            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.MapControllers();
            app.MapDefaultEndpoints();
            app.UseAuthentication();
            app.UseAuthorization();

            await app.RunAsync();
        }

        private static async Task PopulateDefaultRssFeeds(DevForNetDbContext database)
        {
            List<RssFeed> defaultFeeds = new List<RssFeed>()
            {
                new RssFeed()
                {
                    Name = "Microsoft",
                    Url = "https://devblogs.microsoft.com/dotnet/feed/",
                    Tags = new List<string> { "microsoft", "community" },
                    BaseScore = 100,
                    ContentElementType = "div",
                    ContentElementClass = ".entry-content.sharepostcontent",
                },
                new RssFeed()
                {
                    Name = "Telerik",
                    Url = "https://feeds.telerik.com/blogs/web",
                    Tags = new List<string> { "telerik", "ui" },
                    BaseScore = 70,
                    ContentElementType = "div",
                    ContentElementClass = ".sf-Long-text",
                },
                new RssFeed()
                {
                    Name = "JetBrains",
                    Url = "https://blog.jetbrains.com/dotnet/feed/",
                    Tags = new List<string> { "jetbrains", "ide" },
                    BaseScore = 80,
                    ContentElementType = "div",
                    ContentElementClass = ".content.js-toc-content",
                },
                new RssFeed()
                {
                    Name = "Network Programming",
                    Url = "https://blog.dotnetframework.org/feed/",
                    Tags = new List<string> { "networks", "blog" },
                    BaseScore = 50,
                    ContentElementType = "div",
                    ContentElementClass = "#main",
                },
                new RssFeed()
                {
                    Name = "ASP.NET Hacker",
                    Url = "https://asp.net-hacker.rocks/rss.xml",
                    Tags = new List<string> { "asp.net", "blog" },
                    BaseScore = 50,
                    ContentElementType = "div",
                    ContentElementClass = "#main",
                },
                new RssFeed()
                {
                    Name = "DotNetDave (DotNetTips)",
                    Url = "https://dotnettips.wordpress.com/feed/",
                    Tags = new List<string> { "dotnet", "blog" },
                    BaseScore = 50,
                    ContentElementType = "div",
                    ContentElementClass = ".entry-content",
                },
                new RssFeed()
                {
                    Name = "Dave Brock",
                    Url = "https://feeds.feedburner.com/dave-brock",
                    Tags = new List<string> { "dotnet", "blog" },
                    BaseScore = 50,
                    ContentElementType = "div",
                    ContentElementClass = ".post-content",
                },
                new RssFeed()
                {
                    Name = "Rick Strahl",
                    Url = "https://feeds.feedburner.com/RickStrahl",
                    Tags = new List<string> { "dotnet", "blog" },
                    BaseScore = 50,
                    ContentElementType = "div",
                    ContentElementClass = ".post-content",
                },
            };

            await database.RssFeeds.AddRangeAsync(defaultFeeds);
            await database.SaveChangesAsync();
        }

        private static async Task PopulateDefaultCommunities(DevForNetDbContext database)
        {
            List<DotnetCommunity> defaultCommunities = new List<DotnetCommunity>()
            {
                new DotnetCommunity()
                {
                    Name = "devfor.net",
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Description = "devfor.net community discord",
                    Url = "https://discord.gg/eK8HAYeH",
                    ContentType = ContentType.Community,
                },
                new DotnetCommunity()
                {
                    Name = "DotNetEvolution",
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Description = "Huge community Discord related to all things .NET",
                    Url = "https://discord.gg/M5cBTfp6J2",
                    ContentType = ContentType.Community,
                },
                new DotnetCommunity()
                {
                    Name = "FAST",
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Description = "Microsoft's FAST framework community discord.",
                    Url = "https://discord.com/invite/FcSNfg4",
                    ContentType = ContentType.Community,
                },
                new DotnetCommunity()
                {
                    Name = "MudBlazor",
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Description = "MudBlazor community Discord.",
                    Url = "https://discord.gg/w6drbrq4",
                    ContentType = ContentType.Community,
                },
                new DotnetCommunity()
                {
                    Name = "DigitalRuby",
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Description =
                        "Small community built around a developer's open-source projects.",
                    Url = "https://discord.gg/AFhb3rJN",
                    ContentType = ContentType.Community,
                },
            };
            await database.ContentRecords.AddRangeAsync(
                defaultCommunities.Select(t => new ContentRecord()
                {
                    ContentGuid = t.ContentGuid,
                    ContentType = ContentType.Community,
                    PublishedDate = t.PublishedDate,
                    Score = t.Score,
                })
            );
            await database.Communities.AddRangeAsync(defaultCommunities);
            await database.SaveChangesAsync();
        }

        private static async Task PopulateDevForNetArticles(DevForNetDbContext database)
        {
            var devForNetArticles = new List<DevForNetArticle>()
            {
                new DevForNetArticle()
                {
                    ContentType = ContentType.DevForNetArticle,
                    Title = "Sample Article for devfor.net",
                    MarkdownUri = "sample.md",
                    ContentGuid = Guid.CreateVersion7().ToString(),
                    Tags = new List<string>() { "devfornet", "sample" },
                    PublishedDate = DateTime.UtcNow,
                    Score = 100,
                },
            };
            await database.ContentRecords.AddRangeAsync(
                devForNetArticles.Select(t => new ContentRecord()
                {
                    ContentGuid = t.ContentGuid,
                    ContentType = ContentType.DevForNetArticle,
                    PublishedDate = t.PublishedDate,
                    Score = t.Score,
                })
            );
            await database.DevForNetArticles.AddRangeAsync(devForNetArticles);
            await database.SaveChangesAsync();
        }
    }
}

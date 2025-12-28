using devfornet.db;
using devfornet.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace devfornet.ApiService.Repos
{
    public class ContentRepo(IServiceProvider serviceProvider, IMemoryCache contenteCache)
    {
        private readonly IMemoryCache _contentCache = contenteCache;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public async Task<IContent?> GetContentByGuidAsync(ContentType type, string contentGuid)
        {
            return await _contentCache.GetOrCreateAsync<IContent?>(
                $"{type}-{contentGuid}",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    switch (type)
                    {
                        case ContentType.RssArticle:
                            return await LoadRssArticleFromDbAsync(contentGuid);
                        case ContentType.DevForNetArticle:
                            return await LoadDevForNetArticleFromDbAsync(contentGuid);
                        case ContentType.Gist:
                            return await LoadGistFromDbAsync(contentGuid);
                        case ContentType.Community:
                            return await LoadCommunityFromDbAsync(contentGuid);
                        case ContentType.Repo:
                            return await LoadRepoFromDbAsync(contentGuid);
                    }
                    return null;
                }
            );
        }

        private async Task<RssArticle?> LoadRssArticleFromDbAsync(string contentGuid)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
            return await context.RssArticles.FirstOrDefaultAsync(a => a.ContentGuid == contentGuid);
        }

        private async Task<DevForNetArticle?> LoadDevForNetArticleFromDbAsync(string contentGuid)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
            return await context.DevForNetArticles.FirstOrDefaultAsync(a =>
                a.ContentGuid == contentGuid
            );
        }

        private async Task<Gist?> LoadGistFromDbAsync(string contentGuid)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
            return await context.Gists.FirstOrDefaultAsync(a => a.ContentGuid == contentGuid);
        }

        private async Task<DotnetCommunity?> LoadCommunityFromDbAsync(string contentGuid)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
            return await context.Communities.FirstOrDefaultAsync(a => a.ContentGuid == contentGuid);
        }

        private async Task<DotnetRepo?> LoadRepoFromDbAsync(string contentGuid)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
            return await context.Repos.FirstOrDefaultAsync(a => a.ContentGuid == contentGuid);
        }
    }
}

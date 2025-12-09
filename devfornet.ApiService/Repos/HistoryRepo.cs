using devfornet.db;
using devfornet.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace devfornet.ApiService.Repos
{
    public class HistoryRepo(
        ContentRepo contentRepository,
        IServiceProvider serviceProvider,
        IConfiguration config,
        IMemoryCache contentCache
    )
    {
        private readonly int _itemsPerPage = config.GetValue<int>("HistoryItemsPerPage", 20);
        private readonly IMemoryCache _contentCache = contentCache;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ContentRepo _contentRepository = contentRepository;

        public async Task<List<IContent>?> GetAllContentAsync(ContentType type)
        {
            return await _contentCache.GetOrCreateAsync(
                $"{type}-all",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
                    List<ContentRecord> contentRecords = await context
                        .ContentRecords.Where(t => t.ContentType == type)
                        .ToListAsync();
                    List<IContent> content = contentRecords
                        .Select(contentItem =>
                            _contentRepository
                                .GetContentByGuidAsync(
                                    contentItem.ContentType,
                                    contentItem.ContentGuid
                                )
                                .Result
                        )
                        .Where(item => item != null)
                        .ToList()!;
                    _contentCache.Set($"{type}-all", content, TimeSpan.FromMinutes(5));
                    return content;
                }
            );
        }

        public async Task<List<IContent>?> GetPageContentAsync(ContentType type, int page)
        {
            return await _contentCache.GetOrCreateAsync(
                $"{type}-{page}",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
                    List<ContentRecord> contentRecords = await context
                        .ContentRecords.Where(t => t.ContentType == type)
                        .OrderByDescending(c => c.PublishedDate)
                        .Skip(page * _itemsPerPage)
                        .Take(_itemsPerPage)
                        .ToListAsync();
                    List<IContent> content = contentRecords
                        .Select(contentItem =>
                            _contentRepository
                                .GetContentByGuidAsync(
                                    contentItem.ContentType,
                                    contentItem.ContentGuid
                                )
                                .Result
                        )
                        .Where(item => item != null)
                        .ToList()!;
                    _contentCache.Set($"{type}-{page}", content, TimeSpan.FromMinutes(5));
                    return content;
                }
            );
        }

        public async Task<List<IContent>?> GetSortedAllContentAsync(ContentType type)
        {
            return await _contentCache.GetOrCreateAsync(
                $"{type}-all",
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<DevForNetDbContext>();
                    List<ContentRecord> contentRecords = await context
                        .ContentRecords.Where(t => t.ContentType == type)
                        .OrderByDescending(t => t.PublishedDate)
                        .ToListAsync();
                    List<IContent> content = contentRecords
                        .Select(contentItem =>
                            _contentRepository
                                .GetContentByGuidAsync(
                                    contentItem.ContentType,
                                    contentItem.ContentGuid
                                )
                                .Result
                        )
                        .Where(item => item != null)
                        .ToList()!;
                    _contentCache.Set($"{type}-all", content, TimeSpan.FromMinutes(5));
                    return content;
                }
            );
        }
    }
}

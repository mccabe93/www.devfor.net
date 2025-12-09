using devfornet.ApiService.Repos;
using devfornet.db;
using devfornet.Shared.Http.Api;
using devfornet.Shared.Http.Models;
using devfornet.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace devfornet.ApiService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class RssArticlesController(
        IConfiguration config,
        ILogger<RssArticlesController> logger,
        ContentRepo contentRepository,
        HistoryRepo historyRepository
    ) : ControllerBase
    {
        private readonly ContentRepo _contentRepository = contentRepository;
        private readonly HistoryRepo _historyRepository = historyRepository;
        private readonly ILogger<RssArticlesController> _logger = logger;

        private readonly int _itemsPerPage = config.GetValue<int>("HistoryItemsPerPage", 20);

        [HttpGet("headlines/{top}")]
        public async Task<ActionResult<HeadlinesResponse>> GetHeadlines(int top)
        {
            _logger.LogDebug("Fetching headlines from the database.");
            DateTime last24hours = DateTime.UtcNow.AddDays(-1);
            List<IContent> articlesToday = new List<IContent>();
            int page = 0;
            bool readMoreArticles = true;
            do
            {
                var historyEntries = await _historyRepository.GetPageContentAsync(
                    ContentType.RssArticle,
                    page
                );
                if (historyEntries == null || historyEntries.Count == 0)
                    break;
                foreach (var articleEntry in historyEntries)
                {
                    var article = await _contentRepository.GetContentByGuidAsync(
                        articleEntry.ContentType,
                        articleEntry.ContentGuid
                    );
                    if (article == null)
                        continue;
                    if (
                        article.PublishedDate >= last24hours
                        || articlesToday.Count <= _itemsPerPage
                    )
                    {
                        articlesToday.Add(article);
                    }
                    else
                        break;
                }
                page++;
            } while (readMoreArticles);
            var response = new HeadlinesResponse()
            {
                TopRatedArticles = MapContentToHttpModel(
                    articlesToday
                        .OrderByDescending(article => article.PublishedDate)
                        .Take(top)
                        .ToList()
                ),

                LatestArticles = MapContentToHttpModel(
                    articlesToday
                        .OrderByDescending(article => article.PublishedDate)
                        .Take(_itemsPerPage)
                        .ToList()
                ),

                Success = true,
                Message = string.Empty,
            };
            _logger.LogDebug(
                "Retrieved {Count} headlines from the database.",
                response.TopRatedArticles.Count
            );
            return Ok(response);
        }

        private List<HttpArticle> MapContentToHttpModel(List<IContent> contents)
        {
            List<HttpArticle> httpContents = new List<HttpArticle>();
            foreach (var content in contents)
            {
                if (content is RssArticle rssArticle)
                {
                    httpContents.Add(
                        new HttpArticle()
                        {
                            Publisher = rssArticle.Publisher,
                            Title = rssArticle.Title,
                            Url = rssArticle.Url,
                            Tags = rssArticle.Tags,
                            ContentGuid = content.ContentGuid,
                            PublishedDate = content.PublishedDate,
                            Score = content.Score,
                        }
                    );
                }
            }
            return httpContents;
        }
    }
}

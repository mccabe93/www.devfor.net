using devfornet.ApiService.Repos;
using devfornet.Shared.Http.Api;
using devfornet.Shared.Http.Models;
using devfornet.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace devfornet.ApiService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class DevForNetArticlesController(
        IConfiguration config,
        ILogger<DevForNetArticlesController> logger,
        HistoryRepo historyRepository
    ) : ControllerBase
    {
        private readonly HistoryRepo _historyRepository = historyRepository;
        private readonly ILogger<DevForNetArticlesController> _logger = logger;

        [HttpGet("latest/{top}")]
        public async Task<ActionResult<DevForNetArticlesResponse>> GetDevForNetArticles(int top)
        {
            _logger.LogDebug("Fetching DevForNet articles from the database.");
            var devForNetArticles = await _historyRepository.GetAllContentAsync(
                ContentType.DevForNetArticle
            );
            if (devForNetArticles == null || devForNetArticles.Count == 0)
            {
                _logger.LogWarning("No DevForNet articles found in the database.");
                return Ok(
                    new DevForNetArticlesResponse()
                    {
                        Articles = new List<HttpArticle>(),
                        Success = true,
                        Message = "No DevForNet articles found.",
                    }
                );
            }
            var response = new DevForNetArticlesResponse()
            {
                Articles = MapContentToHttpModel(devForNetArticles),
                Success = true,
                Message = string.Empty,
            };
            _logger.LogDebug(
                "Retrieved {Count} DevForNet articles from the database.",
                response.Articles.Count
            );
            return Ok(response);
        }

        private List<HttpArticle> MapContentToHttpModel(List<IContent> contents)
        {
            List<HttpArticle> httpContents = new List<HttpArticle>();
            foreach (var content in contents)
            {
                if (content is DevForNetArticle rssArticle)
                {
                    httpContents.Add(
                        new HttpArticle()
                        {
                            Publisher = "mwmccabe@devfor.net",
                            Title = rssArticle.Title,
                            Url = rssArticle.MarkdownUri,
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

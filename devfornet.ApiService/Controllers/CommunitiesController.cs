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
    public class CommunitiesController(
        IConfiguration config,
        ILogger<CommunitiesController> logger,
        HistoryRepo historyRepository,
        DevForNetDbContext database
    ) : ControllerBase
    {
        private readonly HistoryRepo _historyRepository = historyRepository;
        private readonly DevForNetDbContext _database = database;
        private readonly ILogger<CommunitiesController> _logger = logger;

        [HttpGet("count")]
        public ActionResult<CommunityCountResponse> GetCommunityCount()
        {
            _logger.LogDebug("Fetching community count from the database.");
            var communityCount = _database.Communities.Count();
            _logger.LogDebug("Retrieved community count: {CommunityCount}", communityCount);
            return Ok(
                new CommunityCountResponse()
                {
                    TotalCount = communityCount,
                    Success = true,
                    Message = string.Empty,
                }
            );
        }

        [HttpGet("latest/{count}")]
        public async Task<ActionResult<CommunityLatestResponse>> GetLatest(int count)
        {
            _logger.LogDebug("Fetching latest {Count} communities from the database.", count);
            List<IContent> communities = new List<IContent>();
            int page = 0;

            while (communities.Count < count)
            {
                var pageContent = await _historyRepository.GetPageContentAsync(
                    ContentType.Community,
                    page
                );
                if (pageContent == null || pageContent.Count == 0)
                    break;

                foreach (var content in pageContent)
                {
                    if (content.ContentType == ContentType.Community)
                    {
                        communities.Add(content);
                        if (communities.Count >= count)
                            break;
                    }
                }
                page++;
            }

            var response = new CommunityLatestResponse()
            {
                LatestCommunities = MapContentToHttpModel(
                    communities.OrderByDescending(c => c.PublishedDate).Take(count).ToList()
                ),
                Success = true,
                Message = string.Empty,
            };

            _logger.LogDebug(
                "Retrieved {Count} latest communities.",
                response.LatestCommunities.Count
            );
            return Ok(response);
        }

        [HttpGet("page/{page}")]
        public async Task<ActionResult<CommunityHistoryPageResponse>> GetCommunities(int page)
        {
            _logger.LogDebug("Fetching communities from the database for page {Page}.", page);
            var communities = await _historyRepository.GetPageContentAsync(
                ContentType.Community,
                page
            );

            if (communities == null || communities.Count == 0)
            {
                _logger.LogWarning("No communities found for page {Page}.", page);
                return Ok(
                    new CommunityHistoryPageResponse()
                    {
                        Communities = new List<HttpCommunity>(),
                        TotalCount = 0,
                        Success = true,
                        Message = "No communities found.",
                    }
                );
            }

            _logger.LogDebug("Retrieved {Count} communities from the database.", communities.Count);
            return Ok(
                new CommunityHistoryPageResponse()
                {
                    Communities = MapContentToHttpModel(communities),
                    TotalCount = communities.Count,
                    Success = true,
                    Message = string.Empty,
                }
            );
        }

        private List<HttpCommunity> MapContentToHttpModel(List<IContent> contents)
        {
            List<HttpCommunity> httpContents = new List<HttpCommunity>();
            foreach (var content in contents)
            {
                var httpContent = MapContentToHttpModel(content);
                if (httpContent != null)
                {
                    httpContents.Add(httpContent);
                }
            }
            return httpContents;
        }

        private HttpCommunity? MapContentToHttpModel(IContent? content)
        {
            if (content == null)
                return null;

            if (content is DotnetCommunity community)
            {
                return new HttpCommunity()
                {
                    Publisher = "devfor.net",
                    Description = community.Description,
                    Type = ContentType.Community,
                    Url = community.Url,
                    Title = community.Name,
                    Tags = new List<string> { "community" },
                    ContentGuid = community.ContentGuid,
                    PublishedDate = community.PublishedDate,
                    Score = community.Score,
                };
            }
            return null;
        }
    }
}

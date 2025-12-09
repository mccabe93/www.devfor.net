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
    public class ReposController(
        IConfiguration config,
        ILogger<ReposController> logger,
        HistoryRepo historyRepository,
        DevForNetDbContext database
    ) : ControllerBase
    {
        private readonly HistoryRepo _historyRepository = historyRepository;
        private readonly DevForNetDbContext _database = database;
        private readonly ILogger<ReposController> _logger = logger;

        [HttpGet("count")]
        public ActionResult<RepoCountResponse> GetRepoCount()
        {
            _logger.LogDebug("Fetching repo count from the database.");
            var repoCount = _database.Repos.Count();
            _logger.LogDebug("Retrieved repo count: {RepoCount}", repoCount);
            return Ok(
                new RepoCountResponse()
                {
                    TotalCount = repoCount,
                    Success = true,
                    Message = string.Empty,
                }
            );
        }

        [HttpGet("latest/created/{count}")]
        public async Task<ActionResult<RepoLatestResponse>> GetLatestCreated(int count)
        {
            _logger.LogDebug("Fetching latest {Count} repos from the database.", count);
            var repos = await _historyRepository.GetSortedAllContentAsync(ContentType.Repo);
            if (repos == null || repos.Count == 0)
            {
                _logger.LogWarning("No repos found in the database.");
                return Ok(
                    new RepoLatestResponse()
                    {
                        LatestRepos = new List<HttpRepo>(),
                        Success = true,
                        Message = "No repos found.",
                    }
                );
            }
            var response = new RepoLatestResponse()
            {
                LatestRepos = MapContentToHttpModel(repos.Take(count).ToList()),
                Success = true,
                Message = string.Empty,
            };

            _logger.LogDebug("Retrieved {Count} latest repos.", response.LatestRepos.Count);
            return Ok(response);
        }

        [HttpGet("latest/updated/{count}")]
        public async Task<ActionResult<RepoLatestResponse>> GetLatestUpdated(int count)
        {
            _logger.LogDebug("Fetching latest {Count} repos from the database.", count);
            var repos = await _historyRepository.GetSortedAllContentAsync(ContentType.Repo);
            if (repos == null || repos.Count == 0)
            {
                _logger.LogWarning("No repos found in the database.");
                return Ok(
                    new RepoLatestResponse()
                    {
                        LatestRepos = new List<HttpRepo>(),
                        Success = true,
                        Message = "No repos found.",
                    }
                );
            }
            var response = new RepoLatestResponse()
            {
                LatestRepos = MapContentToHttpModel(
                    repos.OrderByDescending(t => ((DotnetRepo)t).LastUpdated).Take(count).ToList()
                ),
                Success = true,
                Message = string.Empty,
            };

            _logger.LogDebug("Retrieved {Count} latest repos.", response.LatestRepos.Count);
            return Ok(response);
        }

        [HttpGet("latest/noob-friendly/{count}")]
        public async Task<ActionResult<RepoLatestResponse>> GetNoobHelp(int count)
        {
            _logger.LogDebug("Fetching latest {Count} repos from the database.", count);
            var repos = await _historyRepository.GetSortedAllContentAsync(ContentType.Repo);
            if (repos == null || repos.Count == 0)
            {
                _logger.LogWarning("No repos found in the database.");
                return Ok(
                    new RepoLatestResponse()
                    {
                        LatestRepos = new List<HttpRepo>(),
                        Success = true,
                        Message = "No repos found.",
                    }
                );
            }
            var response = new RepoLatestResponse()
            {
                LatestRepos = MapContentToHttpModel(
                    repos.Where(t => ((DotnetRepo)t).NoobFriendly).Take(count).ToList()
                ),
                Success = true,
                Message = string.Empty,
            };

            _logger.LogDebug("Retrieved {Count} latest repos.", response.LatestRepos.Count);
            return Ok(response);
        }

        [HttpGet("latest/help-wanted/{count}")]
        public async Task<ActionResult<RepoLatestResponse>> GetHelpWanted(int count)
        {
            _logger.LogDebug("Fetching latest {Count} repos from the database.", count);
            var repos = await _historyRepository.GetSortedAllContentAsync(ContentType.Repo);
            if (repos == null || repos.Count == 0)
            {
                _logger.LogWarning("No repos found in the database.");
                return Ok(
                    new RepoLatestResponse()
                    {
                        LatestRepos = new List<HttpRepo>(),
                        Success = true,
                        Message = "No repos found.",
                    }
                );
            }
            var response = new RepoLatestResponse()
            {
                LatestRepos = MapContentToHttpModel(
                    repos.Where(t => ((DotnetRepo)t).HelpWanted).Take(count).ToList()
                ),
                Success = true,
                Message = string.Empty,
            };

            _logger.LogDebug("Retrieved {Count} latest repos.", response.LatestRepos.Count);
            return Ok(response);
        }

        [HttpGet("page/{page}")]
        public async Task<ActionResult<RepoHistoryPageResponse>> GetRepos(int page)
        {
            _logger.LogDebug("Fetching repos from the database for page {Page}.", page);
            var repos = await _historyRepository.GetPageContentAsync(ContentType.Repo, page);

            if (repos == null || repos.Count == 0)
            {
                _logger.LogWarning("No repos found for page {Page}.", page);
                return Ok(
                    new RepoHistoryPageResponse()
                    {
                        Repos = new List<HttpRepo>(),
                        TotalCount = 0,
                        Success = true,
                        Message = "No repos found.",
                    }
                );
            }

            _logger.LogDebug("Retrieved {Count} repos from the database.", repos.Count);
            return Ok(
                new RepoHistoryPageResponse()
                {
                    Repos = MapContentToHttpModel(repos),
                    TotalCount = repos.Count,
                    Success = true,
                    Message = string.Empty,
                }
            );
        }

        private List<HttpRepo> MapContentToHttpModel(List<IContent> contents)
        {
            List<HttpRepo> httpContents = new List<HttpRepo>();
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

        private HttpRepo? MapContentToHttpModel(IContent? content)
        {
            if (content == null)
                return null;

            if (content is DotnetRepo repo)
            {
                return new HttpRepo()
                {
                    Description = repo.Description,
                    Publisher = repo.Publisher,
                    Type = ContentType.Repo,
                    Url = repo.Url,
                    Title = repo.Name,
                    Tags = repo.Tags,
                    ContentGuid = repo.ContentGuid,
                    PublishedDate = repo.PublishedDate,
                    Score = repo.Score,
                    Forks = repo.ForkCount,
                    Issues = repo.IssuesCount,
                    Watchers = repo.WatchersCount,
                    HelpWanted = repo.HelpWanted,
                    NoobFriendly = repo.NoobFriendly,
                    LastUpdate = repo.LastUpdated,
                    ManagedByDevForNet = repo.ManagedByDevForNet,
                };
            }
            return null;
        }
    }
}

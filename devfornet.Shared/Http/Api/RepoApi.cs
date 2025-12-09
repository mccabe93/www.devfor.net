using devfornet.Shared.Http.Models;

namespace devfornet.Shared.Http.Api
{
    #region GET responses

    public sealed class RepoCountResponse
    {
        public int TotalCount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class RepoHistoryPageResponse
    {
        public required List<HttpRepo> Repos { get; set; }
        public required int TotalCount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class RepoLatestResponse
    {
        public required List<HttpRepo> LatestRepos { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion GET responses
}

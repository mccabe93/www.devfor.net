using devfornet.Shared.Http.Models;

namespace devfornet.Shared.Http.Api
{
    #region GET responses

    public sealed class CommunityCountResponse
    {
        public int TotalCount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class CommunityHistoryPageResponse
    {
        public required List<HttpCommunity> Communities { get; set; }
        public required int TotalCount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class CommunityLatestResponse
    {
        public required List<HttpCommunity> LatestCommunities { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    #endregion GET responses
}

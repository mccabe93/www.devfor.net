using devfornet.Shared.Http.Models;

namespace devfornet.Shared.Http.Api
{
    public sealed class HeadlinesResponse
    {
        public required List<HttpArticle> TopRatedArticles { get; set; }
        public required List<HttpArticle> LatestArticles { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class DevForNetArticlesResponse
    {
        public required List<HttpArticle> Articles { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    // Not in use. Requires better implementation of ONNX AI model.
    public sealed class ArticleGistResponse
    {
        public HttpGist? Gist { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

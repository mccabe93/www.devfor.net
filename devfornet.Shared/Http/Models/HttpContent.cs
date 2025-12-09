using devfornet.Shared.Models;

namespace devfornet.Shared.Http.Models
{
    public class HttpArticle
    {
        public required string ContentGuid { get; set; }

        /// <summary>
        /// Date the content was published or recorded to database.
        /// </summary>
        public required DateTime PublishedDate { get; set; }

        /// <summary>
        /// Article publisher
        /// </summary>
        public required string Publisher { get; set; }

        /// <summary>
        /// Article title
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Tags associated with the article. By default, inherited from the feed.
        /// </summary>
        public required List<string> Tags { get; set; }

        /// <summary>
        /// Type of content this represents.
        /// </summary>
        public ContentType Type { get; set; } = ContentType.RssArticle;

        /// <summary>
        /// Article URL
        /// </summary>
        public required string Url { get; set; }

        /// <summary>
        /// Community score of the content.
        /// </summary>
        public int Score { get; set; }
    }

    public sealed class HttpCommunity : HttpArticle
    {
        /// <summary>
        /// Description of the community
        /// </summary>
        public required string Description { get; set; }
    }

    public sealed class HttpRepo : HttpArticle
    {
        public required string Description { get; set; }
        public DateTime LastUpdate { get; set; }
        public int Issues { get; set; }
        public int Forks { get; set; }
        public int Watchers { get; set; }
        public bool HelpWanted { get; set; }
        public bool NoobFriendly { get; set; }
        public bool ManagedByDevForNet { get; set; }
    }

    // Not in use. Requires better implementation of ONNX AI model.
    public sealed class HttpGist
    {
        public required string ContentGuid { get; set; }

        /// <summary>
        /// Date the content was published or recorded to database.
        /// </summary>
        public required DateTime PublishedDate { get; set; }

        public required string Content { get; set; }

        /// <summary>
        /// Community score of the content.
        /// </summary>
        public int Score { get; set; }
    }
}

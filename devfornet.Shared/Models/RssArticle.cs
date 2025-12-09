using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devfornet.Shared.Models
{
    public sealed record RssArticle : IContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        public ContentType ContentType { get; init; }

        public DateTime PublishedDate { get; set; }

        public required string ContentGuid { get; set; }

        public int Score { get; set; }

        /// <summary>
        /// Article publisher
        /// </summary>
        public required string Publisher { get; set; }

        /// <summary>
        /// Article title
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Article URL
        /// </summary>
        public required string Url { get; set; }

        /// <summary>
        /// Tags associated with the article. By default, inherited from the feed.
        /// </summary>
        public required List<string> Tags { get; set; }
    }
}

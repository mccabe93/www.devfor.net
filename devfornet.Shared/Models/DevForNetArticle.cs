using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devfornet.Shared.Models
{
    public sealed record DevForNetArticle : IContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        public ContentType ContentType { get; init; }

        public DateTime PublishedDate { get; set; }

        public required string ContentGuid { get; set; }

        public int Score { get; set; }

        /// <summary>
        /// Article title
        /// </summary>
        public required string Title { get; set; }

        /// <summary>
        /// Markdown URI of the article
        /// </summary>
        public required string MarkdownUri { get; set; }

        /// <summary>
        /// Tags associated with the article. By default, inherited from the feed.
        /// </summary>
        public required List<string> Tags { get; set; }
    }
}

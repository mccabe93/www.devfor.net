using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace devfornet.Shared.Models
{
    public sealed record RssFeed
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public int Id { get; init; }

        /// <summary>
        /// Feed's name
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Default tags for articles from this feed.
        /// </summary>
        public required List<string> Tags { get; init; }

        /// <summary>
        /// RSS feed URL
        /// </summary>
        public required string Url { get; init; }

        /// <summary>
        /// Prioritize articles via a baseline score per article.
        /// e.g Microsoft blog posts are more important than a random blog (or at least, we assume so.)
        /// </summary>
        public required int BaseScore { get; set; }

        /// <summary>
        /// WebReaper content element type
        /// </summary>
        public required string ContentElementType { get; init; }

        /// <summary>
        /// WebReaper content element class
        /// </summary>
        public required string ContentElementClass { get; init; }
    }
}

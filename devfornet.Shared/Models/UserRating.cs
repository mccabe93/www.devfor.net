using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace devfornet.Shared.Models
{
    public sealed record UserRating
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [JsonIgnore]
        public int Id { get; set; }

        /// <summary>
        /// JWT token of the user submitting the rating
        /// </summary>
        public required string UserToken { get; set; }

        /// <summary>
        /// Content type to determine the table the article refers to.
        /// </summary>
        public ContentType ContentType { get; set; }

        /// <summary>
        /// The id of the content in its respective table.
        /// </summary>
        public int ContentId { get; set; }
        
        /// <summary>
        /// The rating the user gave.
        /// </summary>
        public int Rating { get; set; }
    }
}

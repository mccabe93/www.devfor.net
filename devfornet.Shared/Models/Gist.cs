using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devfornet.Shared.Models
{
    public sealed record Gist : IContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        public ContentType ContentType { get; init; }

        public DateTime PublishedDate { get; set; }

        public required string ContentGuid { get; set; }

        public int Score { get; set; }

        /// <summary>
        /// The gist content itself.
        /// </summary>
        public required string Content { get; set; }        
        
        /// <summary>
        /// The guid of the content this gist was created from.
        /// </summary>
        public required string ParentContentGuid { get; set; }    
    }
}

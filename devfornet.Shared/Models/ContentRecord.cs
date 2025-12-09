using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devfornet.Shared.Models
{
    public sealed record ContentRecord : IContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        public ContentType ContentType { get; init; }

        public DateTime PublishedDate { get; set; }

        public required string ContentGuid { get; set; }

        public int Score { get; set; }
    }
}

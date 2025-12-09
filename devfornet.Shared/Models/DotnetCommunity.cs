using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace devfornet.Shared.Models
{
    public sealed record DotnetCommunity : IContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }
        public ContentType ContentType { get; init; } = ContentType.Community;
        public DateTime PublishedDate { get; set; }
        public required string ContentGuid { get; set; }
        public int Score { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Url { get; set; }
    }
}

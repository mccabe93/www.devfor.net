using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace devfornet.Shared.Models
{
    public sealed record DotnetRepo : IContent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; init; }

        public ContentType ContentType { get; init; } = ContentType.Event;

        public DateTime PublishedDate { get; set; }

        public int Score { get; set; }

        public string Publisher { get; set; } = string.Empty;

        public required string ContentGuid { get; set; }

        public required string Name { get; set; }

        public required string Description { get; set; }

        public required string Url { get; set; }

        public DateTime LastUpdated { get; set; }

        public int ForkCount { get; set; }

        public int IssuesCount { get; set; }

        public int WatchersCount { get; set; }

        public bool HelpWanted { get; set; }

        public bool NoobFriendly { get; set; }

        public bool ManagedByDevForNet { get; set; }

        /// <summary>
        /// Tags associated with the article. By default, inherited from the feed.
        /// </summary>
        public required List<string> Tags { get; set; }
    }
}

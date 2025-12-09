using System;

namespace devfornet.Shared.Models
{
    public interface IContent
    {
        public int Id { get; init; }

        public ContentType ContentType { get; init; }

        public DateTime PublishedDate { get; set; }

        public string ContentGuid { get; set; }

        public int Score { get; set; }
    }

    public enum ContentType
    {
        Unknown,
        RssArticle,
        DevForNetArticle,
        Gist,
        Community,
        Repo,
        Event,
    }
}

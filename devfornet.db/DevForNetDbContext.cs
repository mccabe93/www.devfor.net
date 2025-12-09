using devfornet.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace devfornet.db
{
    public class DevForNetDbContext(DbContextOptions<DevForNetDbContext> options)
        : DbContext(options)
    {
        public DbSet<ContentRecord> ContentRecords => Set<ContentRecord>();
        public DbSet<RssFeed> RssFeeds => Set<RssFeed>();
        public DbSet<RssArticle> RssArticles => Set<RssArticle>();
        public DbSet<DevForNetArticle> DevForNetArticles => Set<DevForNetArticle>();
        public DbSet<Gist> Gists => Set<Gist>();
        public DbSet<UserRating> UserRatings => Set<UserRating>();
        public DbSet<DotnetCommunity> Communities => Set<DotnetCommunity>();
        public DbSet<DotnetRepo> Repos => Set<DotnetRepo>();
        public DbSet<DotnetEvent> Events => Set<DotnetEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContentRecord>(entity =>
            {
                entity.ToTable("content");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<RssFeed>(entity =>
            {
                entity.ToTable("rssfeed");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<RssArticle>(entity =>
            {
                entity.ToTable("rssarticle");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<DevForNetArticle>(entity =>
            {
                entity.ToTable("devfornetarticle");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<Gist>(entity =>
            {
                entity.ToTable("gist");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<DotnetCommunity>(entity =>
            {
                entity.ToTable("dotnetcommunity");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<DotnetRepo>(entity =>
            {
                entity.ToTable("dotnetrepo");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<DotnetEvent>(entity =>
            {
                entity.ToTable("dotnetevent");
                entity.HasKey(e => e.Id);
            });
            modelBuilder.Entity<UserRating>(entity =>
            {
                entity.ToTable("userrating");
                entity.HasKey(e => e.Id);
            });
        }
    }
}

using InteractHub.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<Follower> Followers => Set<Follower>();
        public DbSet<Hashtag> Hashtags => Set<Hashtag>();
        public DbSet<PostHashtag> PostHashtags => Set<PostHashtag>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Story> Stories => Set<Story>();
        public DbSet<StoryView> StoryViews => Set<StoryView>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User unique constraints
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            // Like unique (one like per user per post)
            modelBuilder.Entity<Like>()
                .HasIndex(l => new { l.UserId, l.PostId }).IsUnique();

            // Follower unique
            modelBuilder.Entity<Follower>()
                .HasIndex(f => new { f.FollowerId, f.FollowingId }).IsUnique();

            // Follower relationships (prevent cascade cycles)
            modelBuilder.Entity<Follower>()
                .HasOne(f => f.FollowerUser)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follower>()
                .HasOne(f => f.FollowingUser)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowingId)
                .OnDelete(DeleteBehavior.Restrict);

            // PostHashtag composite key
            modelBuilder.Entity<PostHashtag>()
                .HasKey(ph => new { ph.PostId, ph.HashtagId });

            // Comment self-referencing
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Post self-referencing (shares)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.SharedPost)
                .WithMany()
                .HasForeignKey(p => p.SharedPostId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification actor
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Actor)
                .WithMany()
                .HasForeignKey(n => n.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Hashtag name unique
            modelBuilder.Entity<Hashtag>()
                .HasIndex(h => h.Name).IsUnique();

            // StoryView unique
            modelBuilder.Entity<StoryView>()
                .HasIndex(sv => new { sv.StoryId, sv.ViewerId }).IsUnique();
        }
    }
}

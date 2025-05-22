using Microsoft.EntityFrameworkCore;
using VideoScripts.Data.Common;
using VideoScripts.Data.Entities;

namespace VideoScripts.Data
{
    /// <summary>
    /// Database context for the VideoScripts application.
    /// This class serves as the primary point of interaction with the underlying database.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets for entities
        public DbSet<ChannelEntity> Channels { get; set; }
        public DbSet<ProjectEntity> Projects { get; set; }
        public DbSet<VideoEntity> Videos { get; set; }
        public DbSet<ScriptEntity> Scripts { get; set; }
        public DbSet<TranscriptTopicEntity> TranscriptTopics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ChannelEntity
            modelBuilder.Entity<ChannelEntity>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Unique constraint on YouTube ID
                entity.HasIndex(e => e.YTId)
                    .IsUnique()
                    .HasDatabaseName("IX_Channels_YTId");

                // Index on Title for searches
                entity.HasIndex(e => e.Title)
                    .HasDatabaseName("IX_Channels_Title");

                // Index on LastCheckDate for maintenance queries
                entity.HasIndex(e => e.LastCheckDate)
                    .HasDatabaseName("IX_Channels_LastCheckDate");

                // Soft delete filter
                entity.HasQueryFilter(e => !e.IsDeleted);

                // Configure relationships
                entity.HasMany(e => e.Videos)
                    .WithOne(v => v.Channel)
                    .HasForeignKey(v => v.ChannelId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
            });

            // Configure ProjectEntity
            modelBuilder.Entity<ProjectEntity>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Index on Name for searches
                entity.HasIndex(e => e.Name)
                    .HasDatabaseName("IX_Projects_Name");

                // Index on Topic for filtering
                entity.HasIndex(e => e.Topic)
                    .HasDatabaseName("IX_Projects_Topic");

                // Soft delete filter
                entity.HasQueryFilter(e => !e.IsDeleted);

                // Configure relationships
                entity.HasMany(e => e.Videos)
                    .WithOne(v => v.Project)
                    .HasForeignKey(v => v.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull); // Allow videos to exist without project

                entity.HasMany(e => e.Scripts)
                    .WithOne(s => s.Project)
                    .HasForeignKey(s => s.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete scripts when project is deleted
            });

            // Configure VideoEntity
            modelBuilder.Entity<VideoEntity>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Unique constraint on YouTube ID
                entity.HasIndex(e => e.YTId)
                    .IsUnique()
                    .HasDatabaseName("IX_Videos_YTId");

                // Index on Title for searches
                entity.HasIndex(e => e.Title)
                    .HasDatabaseName("IX_Videos_Title");

                // Index on PublishedAt for date-based queries
                entity.HasIndex(e => e.PublishedAt)
                    .HasDatabaseName("IX_Videos_PublishedAt");

                // Index on ChannelId for channel-specific queries
                entity.HasIndex(e => e.ChannelId)
                    .HasDatabaseName("IX_Videos_ChannelId");

                // Index on ProjectId for project-specific queries
                entity.HasIndex(e => e.ProjectId)
                    .HasDatabaseName("IX_Videos_ProjectId");

                // Composite index for channel and published date
                entity.HasIndex(e => new { e.ChannelId, e.PublishedAt })
                    .HasDatabaseName("IX_Videos_Channel_PublishedAt");

                // Index on VideoTopic for AI-generated content searches
                entity.HasIndex(e => e.VideoTopic)
                    .HasDatabaseName("IX_Videos_VideoTopic");

                // Soft delete filter
                entity.HasQueryFilter(e => !e.IsDeleted);

                // Configure relationships
                entity.HasOne(e => e.Channel)
                    .WithMany(c => c.Videos)
                    .HasForeignKey(e => e.ChannelId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Videos)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.TranscriptTopics)
                    .WithOne(t => t.Video)
                    .HasForeignKey(t => t.VideoId)
                    .OnDelete(DeleteBehavior.Cascade); // Delete topics when video is deleted
            });

            // Configure ScriptEntity
            modelBuilder.Entity<ScriptEntity>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Index on ProjectId for project-specific queries
                entity.HasIndex(e => e.ProjectId)
                    .HasDatabaseName("IX_Scripts_ProjectId");

                // Index on Title for searches
                entity.HasIndex(e => e.Title)
                    .HasDatabaseName("IX_Scripts_Title");

                // Composite index for project and version
                entity.HasIndex(e => new { e.ProjectId, e.Version })
                    .HasDatabaseName("IX_Scripts_Project_Version");

                // Soft delete filter
                entity.HasQueryFilter(e => !e.IsDeleted);

                // Configure relationships
                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Scripts)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TranscriptTopicEntity
            modelBuilder.Entity<TranscriptTopicEntity>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Index on VideoId for video-specific queries
                entity.HasIndex(e => e.VideoId)
                    .HasDatabaseName("IX_TranscriptTopics_VideoId");

                // Index on StartTime for timeline-based queries
                entity.HasIndex(e => e.StartTime)
                    .HasDatabaseName("IX_TranscriptTopics_StartTime");

                // Composite index for video and start time
                entity.HasIndex(e => new { e.VideoId, e.StartTime })
                    .HasDatabaseName("IX_TranscriptTopics_Video_StartTime");

                // Index on IsSelected for filtering selected topics
                entity.HasIndex(e => e.IsSelected)
                    .HasDatabaseName("IX_TranscriptTopics_IsSelected");

                // Index on Title for searches
                entity.HasIndex(e => e.Title)
                    .HasDatabaseName("IX_TranscriptTopics_Title");

                // Soft delete filter
                entity.HasQueryFilter(e => !e.IsDeleted);

                // Configure relationships
                entity.HasOne(e => e.Video)
                    .WithMany(v => v.TranscriptTopics)
                    .HasForeignKey(e => e.VideoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure BaseEntity properties for all entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                // Set default values for audit fields
                if (typeof(IEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("GETUTCDATE()");

                    modelBuilder.Entity(entityType.ClrType)
                        .Property("LastModifiedAt")
                        .HasDefaultValueSql("GETUTCDATE()");

                    modelBuilder.Entity(entityType.ClrType)
                        .Property("IsDeleted")
                        .HasDefaultValue(false);
                }
            }
        }
    }
}

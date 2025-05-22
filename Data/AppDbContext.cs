using Microsoft.EntityFrameworkCore;

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

        // Add DbSets for your entities here, for example:
        // public DbSet<Video> Videos { get; set; }
        // public DbSet<Transcript> Transcripts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure entity relationships, indexes, and constraints here
        }
    }
}

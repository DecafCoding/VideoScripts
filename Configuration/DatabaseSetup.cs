using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VideoScripts.Data;

namespace VideoScripts.Configuration;

public static class DatabaseSetup
{
    /// <summary>
    /// Initializes and migrates the database
    /// </summary>
    public static async Task InitializeDatabaseAsync(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        using var dbContext = new AppDbContext(dbContextOptions);
        await dbContext.Database.MigrateAsync();
    }
}
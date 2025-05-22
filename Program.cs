using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VideoScripts.Data;

namespace VideoScripts
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Setup configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.develop.json", optional: true, reloadOnChange: true)
                .Build();

            // Configure database context
            var connectionString = config.GetConnectionString("DefaultConnection");
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;
            
            // Create database context
            using var dbContext = new AppDbContext(dbContextOptions);
            
            // Ensure database is created (for development only; use migrations in production)
            dbContext.Database.EnsureCreated();

            // Access configuration values without strongly typed classes
            var greeting = config["AppSettings:Greeting"];
            var environment = config["AppSettings:Environment"];
            Console.WriteLine(greeting);
            Console.WriteLine($"Environment: {environment}");

            // Google Drive/Sheets integration
            var credentialsPath = config["Google:ServiceAccountCredentialsPath"];
            var spreadsheetName = config["Google:SpreadsheetName"];

            if (string.IsNullOrEmpty(credentialsPath) || string.IsNullOrEmpty(spreadsheetName))
            {
                Console.WriteLine("Google credentials path or spreadsheet name not configured.");
                return;
            }

            var googleService = new GoogleDriveService(credentialsPath);
            var spreadsheetId = googleService.FindSpreadsheetIdByName(spreadsheetName);

            if (spreadsheetId == null)
            {
                Console.WriteLine($"Spreadsheet '{spreadsheetName}' not found.");
                return;
            }

            var (lastRow, headers) = googleService.GetLastRow(spreadsheetId);
            if (lastRow == null || headers == null)
            {
                Console.WriteLine("No data found in the spreadsheet.");
                return;
            }

            // Columns to display
            var columns = new[] { "Project Name", "Video 1", "Video 2", "Video 3", "Video 4", "Video 5" };
            Console.WriteLine("Last Row Data:");
            foreach (var col in columns)
            {
                var idx = headers.IndexOf(col);
                var value = (idx >= 0 && idx < lastRow.Count) ? lastRow[idx]?.ToString() : "(empty)";
                Console.WriteLine($"{col}: {value}");
            }
        }
    }
}

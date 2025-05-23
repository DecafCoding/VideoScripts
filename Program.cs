using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VideoScripts.Data;

namespace VideoScripts
{
    internal class Program
    {
        static async Task Main(string[] args)
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

            dbContext.Database.Migrate();

            // Access configuration values
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

            // Get all unimported rows
            var unimportedRows = googleService.GetUnimportedRows(spreadsheetId);

            if (!unimportedRows.Any())
            {
                Console.WriteLine("No unimported rows found in the spreadsheet.");
                return;
            }

            Console.WriteLine($"Found {unimportedRows.Count} unimported row(s):");
            Console.WriteLine(new string('=', 80));

            // Columns to display - now including "Imported"
            var columns = new[] { "Project Name", "Video 1", "Video 2", "Video 3", "Video 4", "Video 5", "Imported" };
            var processedRowNumbers = new List<int>();

            foreach (var (row, rowNumber, headers) in unimportedRows)
            {
                Console.WriteLine($"\nRow {rowNumber} Data:");
                Console.WriteLine(new string('-', 40));

                foreach (var col in columns)
                {
                    var idx = headers.IndexOf(col);
                    var value = (idx >= 0 && idx < row.Count) ? row[idx]?.ToString() : "(empty)";
                    Console.WriteLine($"{col}: {value}");
                }

                // Here you would typically process the row data
                // For demonstration, we'll just simulate processing
                Console.WriteLine("Processing this row...");

                // Simulate some processing work
                await Task.Delay(100);

                // Mark this row for import completion
                processedRowNumbers.Add(rowNumber);

                Console.WriteLine($"Row {rowNumber} processed successfully!");
            }

            // Mark all processed rows as imported in a batch operation
            if (processedRowNumbers.Any())
            {
                Console.WriteLine($"\nMarking {processedRowNumbers.Count} row(s) as imported...");

                var updatedCount = await googleService.MarkRowsAsImportedAsync(
                    spreadsheetId,
                    processedRowNumbers,
                    unimportedRows.First().headers);

                Console.WriteLine($"Successfully marked {updatedCount} cells as imported.");
            }

            Console.WriteLine("\nImport process completed!");
        }
    }
}
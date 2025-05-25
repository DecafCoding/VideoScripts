using Microsoft.Extensions.Configuration;
using VideoScripts.Core;

namespace VideoScripts.Configuration;

public static class GoogleSheetsSetup
{
    /// <summary>
    /// Initializes Google Drive/Sheets service with proper configuration validation
    /// </summary>
    public static GoogleDriveService? InitializeGoogleService(IConfiguration config)
    {
        var credentialsPath = config["Google:ServiceAccountCredentialsPath"];
        var spreadsheetName = config["Google:SpreadsheetName"];

        if (string.IsNullOrEmpty(credentialsPath) || string.IsNullOrEmpty(spreadsheetName))
        {
            ConsoleOutput.DisplayError("Google credentials path or spreadsheet name not configured.");
            return null;
        }

        return new GoogleDriveService(credentialsPath);
    }
}
namespace VideoScripts.Core;

/// <summary>
/// Utility class for extracting data from Google Sheets rows
/// </summary>
public static class SheetDataExtractor
{
    /// <summary>
    /// Gets the value of a specific cell from a Google Sheets row
    /// </summary>
    /// <param name="row">The sheet row data</param>
    /// <param name="headers">Column headers</param>
    /// <param name="columnName">Name of the column to retrieve</param>
    /// <returns>Cell value as string, or empty string if not found</returns>
    public static string GetCellValue(IList<object> row, IList<string> headers, string columnName)
    {
        var columnIndex = headers.IndexOf(columnName);
        if (columnIndex >= 0 && columnIndex < row.Count)
        {
            return row[columnIndex]?.ToString()?.Trim() ?? string.Empty;
        }
        return string.Empty;
    }

    /// <summary>
    /// Extracts all video URLs from a Google Sheets row (Video 1 through Video 7)
    /// </summary>
    /// <param name="row">The sheet row data</param>
    /// <param name="headers">Column headers</param>
    /// <returns>List of video URLs</returns>
    public static List<string> GetVideoUrls(IList<object> row, IList<string> headers)
    {
        return new List<string>
        {
            GetCellValue(row, headers, "Video 1"),
            GetCellValue(row, headers, "Video 2"),
            GetCellValue(row, headers, "Video 3"),
            GetCellValue(row, headers, "Video 4"),
            GetCellValue(row, headers, "Video 5"),
            GetCellValue(row, headers, "Video 6"),
            GetCellValue(row, headers, "Video 7")
        };
    }

    /// <summary>
    /// Gets all video column names for validation or reference
    /// </summary>
    /// <returns>Array of video column names</returns>
    public static string[] GetVideoColumnNames()
    {
        return new[] { "Video 1", "Video 2", "Video 3", "Video 4", "Video 5", "Video 6", "Video 7" };
    }

    /// <summary>
    /// Gets the count of expected video columns
    /// </summary>
    /// <returns>Total number of video columns supported</returns>
    public static int GetVideoColumnCount()
    {
        return 7;
    }
}
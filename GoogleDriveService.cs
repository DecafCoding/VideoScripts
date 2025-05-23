using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace VideoScripts
{
    public class GoogleDriveService
    {
        private readonly string _credentialsPath;
        private readonly string[] _scopes = { DriveService.Scope.DriveReadonly, SheetsService.Scope.Spreadsheets }; // Changed to full spreadsheets scope
        private DriveService _driveService;
        private SheetsService _sheetsService;

        public GoogleDriveService(string credentialsPath)
        {
            _credentialsPath = credentialsPath;
            InitializeServices();
        }

        private void InitializeServices()
        {
            GoogleCredential credential;
            using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(_scopes);
            }

            _driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "VideoScriptsApp"
            });

            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "VideoScriptsApp"
            });
        }

        public string FindSpreadsheetIdByName(string spreadsheetName)
        {
            var request = _driveService.Files.List();
            request.Q = $"mimeType='application/vnd.google-apps.spreadsheet' and name='{spreadsheetName}'";
            request.Fields = "files(id, name)";
            var result = request.Execute();
            return result.Files?.FirstOrDefault()?.Id;
        }

        public (IList<object> row, IList<string> headers) GetLastRow(string spreadsheetId)
        {
            // Get the first sheet's name
            var spreadsheet = _sheetsService.Spreadsheets.Get(spreadsheetId).Execute();
            var sheet = spreadsheet.Sheets.FirstOrDefault();
            if (sheet == null) return (null, null);

            var sheetName = sheet.Properties.Title;

            // Read all data from the sheet
            var range = $"{sheetName}";
            var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;
            if (values == null || values.Count < 2) return (null, null); // No data or only headers

            var headers = values[0].Select(h => h.ToString()).ToList();
            var lastRow = values.LastOrDefault(row => row.Count > 0 && row.Any(cell => !string.IsNullOrEmpty(cell?.ToString())));
            return (lastRow, headers);
        }

        /// <summary>
        /// Gets all rows from the spreadsheet that haven't been imported yet (where "Imported" column is blank or empty)
        /// </summary>
        /// <param name="spreadsheetId">The spreadsheet ID</param>
        /// <returns>List of rows with their row numbers that need to be imported</returns>
        public List<(IList<object> row, int rowNumber, IList<string> headers)> GetUnimportedRows(string spreadsheetId)
        {
            var result = new List<(IList<object> row, int rowNumber, IList<string> headers)>();

            // Get the first sheet's name
            var spreadsheet = _sheetsService.Spreadsheets.Get(spreadsheetId).Execute();
            var sheet = spreadsheet.Sheets.FirstOrDefault();
            if (sheet == null) return result;

            var sheetName = sheet.Properties.Title;

            // Read all data from the sheet
            var range = $"{sheetName}";
            var request = _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);
            var response = request.Execute();
            var values = response.Values;

            if (values == null || values.Count < 2) return result; // No data or only headers

            var headers = values[0].Select(h => h.ToString()).ToList();

            // Find the "Imported" column index
            var importedColumnIndex = headers.IndexOf("Imported");
            if (importedColumnIndex == -1)
            {
                throw new InvalidOperationException("'Imported' column not found in the spreadsheet");
            }

            // Check each row (starting from row 2, index 1) for unimported entries
            for (int i = 1; i < values.Count; i++)
            {
                var row = values[i];

                // Skip empty rows
                if (row.Count == 0 || !row.Any(cell => !string.IsNullOrEmpty(cell?.ToString())))
                    continue;

                // Check if the "Imported" column is blank or empty
                var importedValue = importedColumnIndex < row.Count ? row[importedColumnIndex]?.ToString() : string.Empty;

                if (string.IsNullOrWhiteSpace(importedValue))
                {
                    result.Add((row, i + 1, headers)); // +1 because sheet rows are 1-indexed
                }
            }

            return result;
        }

        /// <summary>
        /// Marks a specific row as imported by setting the "Imported" column to the current timestamp
        /// </summary>
        /// <param name="spreadsheetId">The spreadsheet ID</param>
        /// <param name="rowNumber">The row number to mark as imported (1-indexed)</param>
        /// <param name="headers">The column headers to find the "Imported" column</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> MarkRowAsImportedAsync(string spreadsheetId, int rowNumber, IList<string> headers)
        {
            try
            {
                // Get the first sheet's name
                var spreadsheet = _sheetsService.Spreadsheets.Get(spreadsheetId).Execute();
                var sheet = spreadsheet.Sheets.FirstOrDefault();
                if (sheet == null) return false;

                var sheetName = sheet.Properties.Title;

                // Find the "Imported" column index
                var importedColumnIndex = headers.IndexOf("Imported");
                if (importedColumnIndex == -1)
                {
                    throw new InvalidOperationException("'Imported' column not found in the spreadsheet");
                }

                // Convert column index to A1 notation (A=0, B=1, etc.)
                var columnLetter = GetColumnLetter(importedColumnIndex);
                var cellRange = $"{sheetName}!{columnLetter}{rowNumber}";

                // Prepare the update request
                var valueRange = new ValueRange
                {
                    Range = cellRange,
                    Values = new List<IList<object>>
                    {
                        new List<object> { DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                    }
                };

                var updateRequest = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, cellRange);
                updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                var response = await updateRequest.ExecuteAsync();
                return response.UpdatedCells > 0;
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error marking row {rowNumber} as imported: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Marks multiple rows as imported in a single batch operation
        /// </summary>
        /// <param name="spreadsheetId">The spreadsheet ID</param>
        /// <param name="rowNumbers">List of row numbers to mark as imported (1-indexed)</param>
        /// <param name="headers">The column headers to find the "Imported" column</param>
        /// <returns>Number of successfully updated rows</returns>
        public async Task<int> MarkRowsAsImportedAsync(string spreadsheetId, List<int> rowNumbers, IList<string> headers)
        {
            try
            {
                if (rowNumbers == null || !rowNumbers.Any()) return 0;

                // Get the first sheet's name
                var spreadsheet = _sheetsService.Spreadsheets.Get(spreadsheetId).Execute();
                var sheet = spreadsheet.Sheets.FirstOrDefault();
                if (sheet == null) return 0;

                var sheetName = sheet.Properties.Title;

                // Find the "Imported" column index
                var importedColumnIndex = headers.IndexOf("Imported");
                if (importedColumnIndex == -1)
                {
                    throw new InvalidOperationException("'Imported' column not found in the spreadsheet");
                }

                var columnLetter = GetColumnLetter(importedColumnIndex);
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

                // Prepare batch update request
                var batchUpdateRequest = new BatchUpdateValuesRequest
                {
                    ValueInputOption = "USER_ENTERED",
                    Data = new List<ValueRange>()
                };

                foreach (var rowNumber in rowNumbers)
                {
                    var cellRange = $"{sheetName}!{columnLetter}{rowNumber}";
                    batchUpdateRequest.Data.Add(new ValueRange
                    {
                        Range = cellRange,
                        Values = new List<IList<object>>
                        {
                            new List<object> { timestamp }
                        }
                    });
                }

                var response = await _sheetsService.Spreadsheets.Values.BatchUpdate(batchUpdateRequest, spreadsheetId).ExecuteAsync();
                return response.TotalUpdatedCells ?? 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking rows as imported: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Converts a zero-based column index to Excel-style column letter (A, B, C, ..., AA, AB, etc.)
        /// </summary>
        /// <param name="columnIndex">Zero-based column index</param>
        /// <returns>Column letter(s)</returns>
        private string GetColumnLetter(int columnIndex)
        {
            string columnLetter = string.Empty;

            while (columnIndex >= 0)
            {
                columnLetter = (char)('A' + (columnIndex % 26)) + columnLetter;
                columnIndex = (columnIndex / 26) - 1;
            }

            return columnLetter;
        }
    }
}
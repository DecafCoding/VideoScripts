using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace VideoScripts
{
    public class GoogleDriveService
    {
        private readonly string _credentialsPath;
        private readonly string[] _scopes = { DriveService.Scope.DriveReadonly, SheetsService.Scope.SpreadsheetsReadonly };
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
    }
}

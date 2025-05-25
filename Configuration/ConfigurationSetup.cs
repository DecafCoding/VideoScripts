using Microsoft.Extensions.Configuration;

namespace VideoScripts.Configuration;

public static class ConfigurationSetup
{
    /// <summary>
    /// Builds the application configuration from appsettings files
    /// </summary>
    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.develop.json", optional: true, reloadOnChange: true)
            .Build();
    }
}
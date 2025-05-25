using Microsoft.Extensions.Configuration;

namespace VideoScripts.Core;

/// <summary>
/// Handles all console output formatting and display logic
/// </summary>
public static class ConsoleOutput
{
    /// <summary>
    /// Displays startup information from configuration
    /// </summary>
    public static void DisplayStartupInfo(IConfiguration config)
    {
        var greeting = config["AppSettings:Greeting"];
        var environment = config["AppSettings:Environment"];

        Console.WriteLine(greeting);
        Console.WriteLine($"Environment: {environment}");
    }

    /// <summary>
    /// Displays error messages in a consistent format
    /// </summary>
    public static void DisplayError(string message)
    {
        Console.WriteLine($"ERROR: {message}");
    }

    /// <summary>
    /// Displays info messages in a consistent format
    /// </summary>
    public static void DisplayInfo(string message)
    {
        Console.WriteLine($"INFO: {message}");
    }

    /// <summary>
    /// Displays section headers with separators
    /// </summary>
    public static void DisplaySectionHeader(string title, char separator = '=', int width = 80)
    {
        Console.WriteLine(new string(separator, width));
        Console.WriteLine(title);
        Console.WriteLine(new string(separator, width));
    }

    /// <summary>
    /// Displays subsection headers with separators
    /// </summary>
    public static void DisplaySubsectionHeader(string title, int width = 50)
    {
        Console.WriteLine($"\n{title}");
        Console.WriteLine(new string('-', width));
    }

    /// <summary>
    /// Gets user input with a prompt
    /// </summary>
    public static string? GetUserInput(string prompt)
    {
        Console.WriteLine(prompt);
        return Console.ReadLine();
    }
}
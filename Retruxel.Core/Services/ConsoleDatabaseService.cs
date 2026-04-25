using Retruxel.Core.Models;
using System.Text.Json;

namespace Retruxel.Core.Services;

public static class ConsoleDatabaseService
{
    private static List<ConsoleSpec>? _consoles;

    public static void Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            return;

        var json = File.ReadAllText(jsonPath);
        var root = JsonSerializer.Deserialize<ConsoleDatabase>(json);
        _consoles = root?.Consoles ?? new List<ConsoleSpec>();
    }

    public static ConsoleSpec? FindByName(string name)
    {
        return _consoles?.FirstOrDefault(c => c.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    public static List<string> GetAllConsoleNames()
    {
        return _consoles?.Select(c => c.Name).OrderBy(n => n).ToList() ?? new List<string>();
    }

    public static List<ConsoleSpec> SearchConsoles(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _consoles ?? new List<ConsoleSpec>();

        return _consoles?.Where(c =>
            c.Name.Contains(query, System.StringComparison.OrdinalIgnoreCase) ||
            c.Manufacturer.Contains(query, System.StringComparison.OrdinalIgnoreCase)
        ).ToList() ?? new List<ConsoleSpec>();
    }

    private class ConsoleDatabase
    {
        public List<ConsoleSpec> Consoles { get; set; } = new();
    }
}

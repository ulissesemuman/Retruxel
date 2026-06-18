using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Retruxel.Core.Services;

/// <summary>
/// Discovers and indexes ActionDefinitions from Plugins/CodeGens/actions/**/action.json.
///
/// Directory layout:
///   actions/{actionId}/all/action.json       — target-agnostic variant
///   actions/{actionId}/{targetId}/action.json — target-specific variant (overrides all/)
///
/// Discovery priority per action + target:
///   1. actions/{actionId}/{targetId}/action.json
///   2. actions/{actionId}/all/action.json
/// </summary>
public class ActionRegistry
{
    private readonly Dictionary<string, ActionDefinition> _actions;
    private readonly Dictionary<string, string> _templatePaths;
    private readonly Dictionary<string, List<EventEmitDef>> _emits; // actionId → emits

    private ActionRegistry(
        Dictionary<string, ActionDefinition> actions,
        Dictionary<string, string> templatePaths,
        Dictionary<string, List<EventEmitDef>> emits)
    {
        _actions = actions;
        _templatePaths = templatePaths;
        _emits = emits;
    }

    /// <summary>All discovered actions, keyed by ActionId.</summary>
    public IReadOnlyDictionary<string, ActionDefinition> Actions => _actions;

    /// <summary>
    /// Returns all events emitted by a given action.
    /// Used by the editor to populate the event dropdown in the binding UI.
    /// </summary>
    public IReadOnlyList<EventEmitDef> GetEmits(string actionId)
        => _emits.TryGetValue(actionId, out var list) ? list : [];

    /// <summary>
    /// Returns all events emitted across all active actions in a project.
    /// Deduplicated by eventId.
    /// </summary>
    public IReadOnlyList<EventEmitDef> GetAllEmits(IEnumerable<string> activeActionIds)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<EventEmitDef>();
        foreach (var actionId in activeActionIds)
        {
            foreach (var emit in GetEmits(actionId))
            {
                if (seen.Add(emit.EventId))
                    result.Add(emit);
            }
        }
        return result;
    }

    public ActionDefinition? GetById(string actionId)
        => _actions.TryGetValue(actionId, out var def) ? def : null;

    /// <summary>
    /// Returns all transitive dependencies for a given actionId (recursive, de-duplicated).
    /// The returned list is ordered so dependencies come before dependents.
    /// </summary>
    public IReadOnlyList<string> GetAllDependencies(string actionId)
    {
        var result  = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectDependencies(actionId, result, visited);
        return result;
    }

    private void CollectDependencies(string actionId, List<string> result, HashSet<string> visited)
    {
        if (!_actions.TryGetValue(actionId, out var def)) return;
        foreach (var dep in def.Dependencies)
        {
            if (!visited.Add(dep)) continue;
            CollectDependencies(dep, result, visited); // depth-first
            result.Add(dep);
        }
    }

    /// <summary>
    /// Returns the template path for a given actionId and targetId.
    /// Falls back to the "all" variant if no target-specific template exists.
    /// Returns null if no template is found.
    /// </summary>
    public string? GetTemplatePath(string actionId, string targetId)
    {
        var specificKey = Key(actionId, targetId);
        if (_templatePaths.TryGetValue(specificKey, out var specific))
            return specific;

        var allKey = Key(actionId, "all");
        return _templatePaths.TryGetValue(allKey, out var fallback) ? fallback : null;
    }

    /// <summary>
    /// Scans Plugins/CodeGens/actions/ and builds an ActionRegistry from all action.json files.
    /// </summary>
    public static ActionRegistry Discover(string pluginsPath, IProgress<string>? progress = null)
    {
        var actions = new Dictionary<string, ActionDefinition>(StringComparer.OrdinalIgnoreCase);
        var templatePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var emits = new Dictionary<string, List<EventEmitDef>>(StringComparer.OrdinalIgnoreCase);

        var actionsDir = Path.Combine(pluginsPath, "CodeGens", "actions");
        if (!Directory.Exists(actionsDir))
            return new ActionRegistry(actions, templatePaths);

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        foreach (var manifestPath in Directory.GetFiles(actionsDir, "action.json", SearchOption.AllDirectories))
        {
            try
            {
                var raw = JsonSerializer.Deserialize<ActionManifestRaw>(
                    File.ReadAllText(manifestPath), opts);

                if (raw?.ActionId is null) continue;

                // The target variant is the name of the immediate parent folder (e.g. "all", "sms")
                var variantDir = Path.GetDirectoryName(manifestPath)!;
                var targetVariant = Path.GetFileName(variantDir);

                // Resolve template path — by convention, same folder as action.json
                var templateFile = Directory.GetFiles(variantDir, "*.c.rtrx").FirstOrDefault();

                var def = new ActionDefinition(
                    ActionId:     raw.ActionId,
                    DisplayName:  raw.DisplayName ?? raw.ActionId,
                    Category:     raw.Category    ?? "General",
                    Parameters:   ParseParameters(raw.Parameters),
                    Dependencies: raw.Dependencies?.ToArray() ?? [],
                    Scope:        raw.Scope ?? "entity"
                );

                // Register definition keyed by actionId
                if (!actions.ContainsKey(raw.ActionId))
                    actions[raw.ActionId] = def;

                // Register emits
                if (raw.Emits is { Count: > 0 })
                    emits[raw.ActionId] = raw.Emits
                        .Select(e => new EventEmitDef { EventId = e.EventId ?? string.Empty, Label = e.Label ?? e.EventId ?? string.Empty })
                        .ToList();

                // Register template path keyed by actionId::targetVariant
                if (templateFile is not null)
                    templatePaths[Key(raw.ActionId, targetVariant)] = templateFile;
            }
            catch (Exception ex)
            {
                progress?.Report($"WARN: Failed to load action manifest {manifestPath}: {ex.Message}");
            }
        }

        progress?.Report($"INFO: ActionRegistry — {actions.Count} action(s) discovered.");
        return new ActionRegistry(actions, templatePaths, emits);
    }

    private static ActionParameterDef[] ParseParameters(List<ActionParameterDefRaw>? raw)
    {
        if (raw is null) return [];

        return raw.Select(p =>
        {
            // Detect pipe-separated options in the default string: "patrol|walk|idle"
            string[] options = [];
            string? defaultStr = p.Default?.ValueKind == JsonValueKind.String
                ? p.Default.Value.GetString() : null;
            if (defaultStr is not null && defaultStr.Contains('|'))
            {
                options = defaultStr.Split('|');
                defaultStr = options[0]; // first option is the actual default
            }

            var parsedDefault = options.Length > 0
                ? (object)(defaultStr ?? string.Empty)
                : ParseDefault(p.Default, p.Type);

            return new ActionParameterDef(
                Name:      p.Name      ?? string.Empty,
                Type:      p.Type      ?? "string",
                Default:   parsedDefault,
                Label:     p.Label     ?? p.Name ?? string.Empty,
                EntityRef: p.EntityRef,
                Options:   options
            );
        }).ToArray();
    }

    private static object ParseDefault(JsonElement? element, string? type)
    {
        if (element is null) return string.Empty;

        return element.Value.ValueKind switch
        {
            JsonValueKind.True    => true,
            JsonValueKind.False   => false,
            JsonValueKind.Number  => element.Value.TryGetInt32(out var i) ? (object)i : element.Value.GetDouble(),
            JsonValueKind.String  => element.Value.GetString() ?? string.Empty,
            _                     => string.Empty
        };
    }

    private static string Key(string actionId, string targetVariant)
        => $"{actionId}::{targetVariant}".ToLowerInvariant();

    // ── Raw deserialization models ─────────────────────────────────────────────

    private class ActionManifestRaw
    {
        public string? ActionId          { get; set; }
        public string? DisplayName       { get; set; }
        public string? Category          { get; set; }
        public string? Scope             { get; set; }
        public List<string>? Dependencies { get; set; }
        public List<ActionParameterDefRaw>? Parameters { get; set; }
        public List<EventEmitRaw>? Emits  { get; set; }
    }

    private class EventEmitRaw
    {
        public string? EventId { get; set; }
        public string? Label   { get; set; }
    }

    private class ActionParameterDefRaw
    {
        public string?       Name      { get; set; }
        public string?       Type      { get; set; }
        public JsonElement?  Default   { get; set; }
        public string?       Label     { get; set; }
        public bool          EntityRef { get; set; }
    }
}

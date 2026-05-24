using Retruxel.Core.Interfaces;
using Retruxel.Core.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Retruxel.Core.Services;

/// <summary>
/// Module processing: context injection, code generation per module.
/// </summary>
public partial class CodeGenerator
{
    /// <summary>
    /// Wraps a module so that its Serialize() output includes context flags
    /// about which other modules are present in the project.
    ///
    /// Current flags injected:
    ///   entity → "usePhysics", "useInput", "useAnimation"
    ///   enemy  → "useAnimation"
    ///   physics → "usePlane"
    ///
    /// The wrapper only modifies Serialize() — all other IModule members
    /// delegate to the original module unchanged.
    /// </summary>
    private static IModule InjectContextFlags(IModule module, HashSet<string> presentModuleIds)
    {
        if (module.ModuleId == "entity")
        {
            var usePhysics = presentModuleIds.Contains("physics");
            var useInput = presentModuleIds.Contains("input");
            var useAnimation = presentModuleIds.Contains("animation");

            if (usePhysics || useInput || useAnimation)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["usePhysics"] = usePhysics,
                    ["useInput"] = useInput,
                    ["useAnimation"] = useAnimation
                }));
        }

        if (module.ModuleId == "enemy")
        {
            var useAnimation = presentModuleIds.Contains("animation");

            if (useAnimation)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["useAnimation"] = useAnimation
                }));
        }

        if (module.ModuleId == "physics")
        {
            var usePlane = presentModuleIds.Contains("plane");

            if (usePlane)
                return new ContextualModule(module, json => InjectFlags(json, new()
                {
                    ["usePlane"] = usePlane
                }));
        }

        return module;
    }

    /// <summary>
    /// Parses a JSON string, merges extra flags into the root object, and returns
    /// the modified JSON string.
    /// </summary>
    private static string InjectFlags(string json, Dictionary<string, bool> flags)
    {
        try
        {
            var node = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
            foreach (var (key, value) in flags)
                node[key] = value;
            return node.ToJsonString();
        }
        catch
        {
            return json; // If parsing fails, return original unchanged
        }
    }

    /// <summary>
    /// Wraps an IModule, overriding Serialize() to inject additional JSON properties.
    /// All other members delegate to the wrapped module.
    /// </summary>
    private sealed class ContextualModule(IModule inner, Func<string, string> serializeTransform) : IModule
    {
        public string ModuleId => inner.ModuleId;
        public string DisplayName => inner.DisplayName;
        public string Category => inner.Category;
        public ModuleType Type => inner.Type;
        public string[] Compatibility => inner.Compatibility;
        public SingletonPolicy SingletonPolicy => inner.SingletonPolicy;
        public ModuleScope DefaultScope => inner.DefaultScope;
        public string Serialize() => serializeTransform(inner.Serialize());
        public void Deserialize(string json) => inner.Deserialize(json);
        public string GetValidationSample() => inner.GetValidationSample();
    }
}

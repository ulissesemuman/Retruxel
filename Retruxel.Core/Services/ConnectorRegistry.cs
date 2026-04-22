using Retruxel.Core.Interfaces;

namespace Retruxel.Core.Services;

/// <summary>
/// Registry for tool connectors with default mappings per execution context
/// </summary>
public class ConnectorRegistry
{
    private readonly Dictionary<Models.ExecutionContext, List<IToolConnector>> _defaults = new();
    private readonly Dictionary<string, IToolConnector> _registered = new();

    /// <summary>
    /// Registers a connector instance
    /// </summary>
    public void Register(IToolConnector connector)
    {
        _registered[connector.ConnectorId] = connector;
    }

    /// <summary>
    /// Registers a connector as default for a specific execution context
    /// </summary>
    public void RegisterDefault(IToolConnector connector, Models.ExecutionContext context)
    {
        if (!_defaults.ContainsKey(context))
            _defaults[context] = new();

        _defaults[context].Add(connector);
    }

    /// <summary>
    /// Gets default connectors for a specific execution context
    /// </summary>
    public IEnumerable<IToolConnector> GetDefaults(Models.ExecutionContext context)
    {
        return _defaults.TryGetValue(context, out var connectors)
            ? connectors
            : Enumerable.Empty<IToolConnector>();
    }

    /// <summary>
    /// Gets a registered connector by ID
    /// </summary>
    public IToolConnector? GetById(string connectorId)
    {
        return _registered.TryGetValue(connectorId, out var connector) ? connector : null;
    }

    /// <summary>
    /// Gets all registered connectors
    /// </summary>
    public IEnumerable<IToolConnector> GetAll()
    {
        return _registered.Values;
    }
}

namespace Impulse.Core;

/// <summary>
/// Registry of all Impulse component and mutation types for code generation.
/// </summary>
public sealed class ImpulseTypeRegistry
{
    private readonly Dictionary<string, ImpulseComponentMetadata> _components = new();
    private readonly Dictionary<string, ImpulseMutationMetadata> _mutations = new();
    private readonly HashSet<Type> _propsTypes = new();
    private readonly HashSet<Type> _requestTypes = new();
    private readonly HashSet<Type> _responseTypes = new();

    /// <summary>
    /// Registers a component.
    /// </summary>
    public void RegisterComponent(string route, ImpulseComponentMetadata metadata)
    {
        _components[route] = metadata;
        _propsTypes.Add(metadata.PropsType);

        foreach (var deferred in metadata.Deferred.Values)
        {
            _propsTypes.Add(deferred.PropsType);
        }

        foreach (var lazy in metadata.Lazy.Values)
        {
            _propsTypes.Add(lazy.PropsType);
        }
    }

    /// <summary>
    /// Registers a mutation.
    /// </summary>
    public void RegisterMutation(string route, ImpulseMutationMetadata metadata)
    {
        _mutations[route] = metadata;
        _requestTypes.Add(metadata.RequestType);
        _responseTypes.Add(metadata.ResponseType);
    }

    /// <summary>
    /// Gets all registered components.
    /// </summary>
    public IReadOnlyDictionary<string, ImpulseComponentMetadata> Components => _components;

    /// <summary>
    /// Gets all registered mutations.
    /// </summary>
    public IReadOnlyDictionary<string, ImpulseMutationMetadata> Mutations => _mutations;

    /// <summary>
    /// Gets all props types that need TypeScript generation.
    /// </summary>
    public IReadOnlySet<Type> PropsTypes => _propsTypes;

    /// <summary>
    /// Gets all request types that need TypeScript generation.
    /// </summary>
    public IReadOnlySet<Type> RequestTypes => _requestTypes;

    /// <summary>
    /// Gets all response types that need TypeScript generation.
    /// </summary>
    public IReadOnlySet<Type> ResponseTypes => _responseTypes;

    /// <summary>
    /// Gets all types that need TypeScript generation.
    /// </summary>
    public IEnumerable<Type> AllTypes =>
        _propsTypes.Concat(_requestTypes).Concat(_responseTypes).Distinct();
}

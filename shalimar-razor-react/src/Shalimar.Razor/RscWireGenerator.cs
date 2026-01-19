using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Shalimar.Razor;

/// <summary>
/// Generates React Server Components wire format from compiled Razor data.
///
/// RSC Wire Format (React 19):
/// - Line-delimited JSON
/// - Element format: ["$","tagName",key,{props}]
/// - $L prefix = lazy/client component reference
/// - Server components serialize to the wire format
/// - Client components get a reference marker
/// </summary>
public class RscWireGenerator
{
    private int _lineId = 0;
    private readonly StringBuilder _output = new();
    private readonly Dictionary<string, bool> _clientComponents = new();

    /// <summary>
    /// Generate RSC wire format payload from component tree data.
    /// </summary>
    public string GeneratePayload(string rootComponent, Dictionary<string, object?> props,
        Dictionary<string, bool> componentTypes)
    {
        _lineId = 0;
        _output.Clear();
        _clientComponents.Clear();

        foreach (var (name, isClient) in componentTypes)
        {
            _clientComponents[name] = isClient;
        }

        // Generate the component tree as RSC wire format
        var rootElement = CreateElement(rootComponent, props);
        var rootJson = JsonSerializer.Serialize(rootElement);

        _output.Insert(0, $"0:{rootJson}\n");

        return _output.ToString();
    }

    /// <summary>
    /// Create an RSC element representation.
    /// Format: ["$", "tagName", key, {props}]
    /// </summary>
    private object?[] CreateElement(string tagName, Dictionary<string, object?>? props = null, string? key = null)
    {
        var isClient = _clientComponents.GetValueOrDefault(tagName, false);

        if (isClient)
        {
            // Client component: reference marker for hydration
            // Format: ["$", "$Lclient_id", key, {props}]
            var clientId = $"$L{tagName}";
            return new object?[] { "$", clientId, key, SerializeProps(props) };
        }

        // Server component or HTML element: fully serialize
        return new object?[] { "$", tagName, key, SerializeProps(props) };
    }

    private Dictionary<string, object?>? SerializeProps(Dictionary<string, object?>? props)
    {
        if (props == null || props.Count == 0) return null;

        var result = new Dictionary<string, object?>();

        foreach (var (key, value) in props)
        {
            result[key] = SerializeValue(value);
        }

        return result;
    }

    private object? SerializeValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            bool b => b,
            int i => i,
            double d => d,
            float f => f,
            long l => l,
            Dictionary<string, object?> dict => SerializeProps(dict),
            IEnumerable<object?> list => list.Select(SerializeValue).ToArray(),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Generate the RSC client runtime that interprets wire format.
    /// This runs in the browser and constructs React elements from the payload.
    /// </summary>
    public static string GenerateClientRuntime()
    {
        return @"
// RSC Wire Format Client Runtime
// Interprets server-generated wire format and creates React elements

window.__RSC_RUNTIME__ = {
    // Parse RSC wire format payload
    parsePayload(payload) {
        const lines = payload.trim().split('\n');
        const elements = {};

        for (const line of lines) {
            const colonIdx = line.indexOf(':');
            const id = line.substring(0, colonIdx);
            const data = JSON.parse(line.substring(colonIdx + 1));
            elements[id] = data;
        }

        return elements;
    },

    // Convert RSC element to React element
    createElement(data, components) {
        if (!Array.isArray(data) || data[0] !== '$') {
            return data; // Plain value (string, number, etc.)
        }

        const [marker, type, key, props] = data;

        // Handle client component reference ($Lname)
        if (typeof type === 'string' && type.startsWith('$L')) {
            const componentName = type.substring(2);
            const Component = components[componentName];
            if (!Component) {
                console.warn(`Client component not found: ${componentName}`);
                return null;
            }
            return React.createElement(Component, { key, ...this.resolveProps(props, components) });
        }

        // Resolve component or use tag name
        const Component = components[type] || type;
        const resolvedProps = this.resolveProps(props, components);

        return React.createElement(Component, { key, ...resolvedProps });
    },

    // Resolve props, converting nested RSC elements
    resolveProps(props, components) {
        if (!props) return {};

        const resolved = {};
        for (const [key, value] of Object.entries(props)) {
            if (Array.isArray(value) && value[0] === '$') {
                resolved[key] = this.createElement(value, components);
            } else if (Array.isArray(value)) {
                resolved[key] = value.map(v =>
                    Array.isArray(v) && v[0] === '$' ? this.createElement(v, components) : v
                );
            } else {
                resolved[key] = value;
            }
        }
        return resolved;
    },

    // Render RSC payload to DOM
    render(payload, containerId, components) {
        const elements = this.parsePayload(payload);
        const rootData = elements['0'];
        const rootElement = this.createElement(rootData, components);
        const container = document.getElementById(containerId);
        ReactDOM.createRoot(container).render(rootElement);
    }
};
";
    }
}

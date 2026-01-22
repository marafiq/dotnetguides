namespace Shalimar.Razor;

/// <summary>
/// Enhanced DSL for defining React components with clear separation of concerns.
/// State, Props, Actions, Computed values all defined in C# with type safety.
/// </summary>
public class Component
{
    private readonly ComponentDefinition _def;

    private Component(string name)
    {
        _def = new ComponentDefinition { Name = name };
    }

    public static Component Define(string name) => new(name);

    public Component Client()
    {
        _def.IsClient = true;
        return this;
    }

    public Component Server()
    {
        _def.IsServer = true;
        return this;
    }

    public Component State(Action<StateBuilder> configure)
    {
        var builder = new StateBuilder();
        configure(builder);
        _def.StateFields = builder.Build();
        return this;
    }

    public Component Props(Action<PropsBuilder> configure)
    {
        var builder = new PropsBuilder();
        configure(builder);
        _def.PropFields = builder.Build();
        return this;
    }

    public Component Actions(Action<ActionsBuilder> configure)
    {
        var builder = new ActionsBuilder();
        configure(builder);
        _def.ActionDefs = builder.Build();
        return this;
    }

    public Component Computed(Action<ComputedBuilder> configure)
    {
        var builder = new ComputedBuilder();
        configure(builder);
        _def.ComputedFields = builder.Build();
        return this;
    }

    public Component Import(string path, params string[] names)
    {
        _def.Imports.Add(new ImportDef
        {
            Path = path,
            NamedImports = names.Length > 0 ? names.ToList() : null
        });
        return this;
    }

    public Component Render(string template)
    {
        _def.Template = template;
        return this;
    }

    public Component Render(Func<RenderContext, string> templateFunc)
    {
        // Create a context that can be used in string interpolation
        _def.TemplateFunc = templateFunc;
        return this;
    }

    public string Generate()
    {
        var emitter = new EnhancedEmitter();
        return emitter.Emit(_def);
    }

    public ComponentDefinition Build() => _def;
}

/// <summary>
/// Builder for state fields (TanStack Store)
/// </summary>
public class StateBuilder
{
    private readonly List<FieldDef> _fields = new();

    public StateBuilder Field(string name, string defaultValue)
    {
        _fields.Add(new FieldDef
        {
            Name = name,
            TsType = "string",
            DefaultValue = $"\"{defaultValue}\""
        });
        return this;
    }

    public StateBuilder Field(string name, int defaultValue)
    {
        _fields.Add(new FieldDef
        {
            Name = name,
            TsType = "number",
            DefaultValue = defaultValue.ToString()
        });
        return this;
    }

    public StateBuilder Field(string name, bool defaultValue)
    {
        _fields.Add(new FieldDef
        {
            Name = name,
            TsType = "boolean",
            DefaultValue = defaultValue ? "true" : "false"
        });
        return this;
    }

    public StateBuilder Field<T>(string name, string tsType, string defaultValue)
    {
        _fields.Add(new FieldDef
        {
            Name = name,
            TsType = tsType,
            DefaultValue = defaultValue
        });
        return this;
    }

    public StateBuilder Array<T>(string name) where T : class
    {
        var itemType = typeof(T).Name;
        _fields.Add(new FieldDef
        {
            Name = name,
            TsType = $"{itemType}[]",
            DefaultValue = "[]"
        });
        return this;
    }

    public List<FieldDef> Build() => _fields;
}

/// <summary>
/// Builder for props (component parameters)
/// </summary>
public class PropsBuilder
{
    private readonly List<PropDef> _props = new();

    public PropsBuilder Required<T>(string name)
    {
        _props.Add(new PropDef
        {
            Name = name,
            TsType = GetTsType<T>(),
            IsOptional = false
        });
        return this;
    }

    public PropsBuilder Optional<T>(string name, T defaultValue)
    {
        _props.Add(new PropDef
        {
            Name = name,
            TsType = GetTsType<T>(),
            IsOptional = true,
            DefaultValue = FormatDefault(defaultValue)
        });
        return this;
    }

    public PropsBuilder Children()
    {
        _props.Add(new PropDef
        {
            Name = "children",
            TsType = "React.ReactNode",
            IsOptional = true
        });
        return this;
    }

    private static string GetTsType<T>()
    {
        var type = typeof(T);
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(double)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var inner = type.GetGenericArguments()[0];
            return $"{GetTsTypeFromType(inner)}[]";
        }
        return type.Name;
    }

    private static string GetTsTypeFromType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(double)) return "number";
        if (type == typeof(bool)) return "boolean";
        return type.Name;
    }

    private static string FormatDefault<T>(T value)
    {
        if (value is string s) return $"\"{s}\"";
        if (value is bool b) return b ? "true" : "false";
        return value?.ToString() ?? "null";
    }

    public List<PropDef> Build() => _props;
}

/// <summary>
/// Builder for actions (event handlers that mutate state)
/// </summary>
public class ActionsBuilder
{
    private readonly List<ActionDef> _actions = new();

    /// <summary>
    /// Define action with mutation expression (C# syntax that gets transformed)
    /// </summary>
    public ActionsBuilder Define(string name, string mutation, bool isAsync = false)
    {
        _actions.Add(new ActionDef
        {
            Name = name,
            Mutation = mutation,
            IsAsync = isAsync
        });
        return this;
    }

    /// <summary>
    /// Define action with parameters
    /// </summary>
    public ActionsBuilder Define(string name, string parameters, string mutation, bool isAsync = false)
    {
        _actions.Add(new ActionDef
        {
            Name = name,
            Parameters = parameters,
            Mutation = mutation,
            IsAsync = isAsync
        });
        return this;
    }

    public List<ActionDef> Build() => _actions;
}

/// <summary>
/// Builder for computed/derived values
/// </summary>
public class ComputedBuilder
{
    private readonly List<ComputedDef> _computed = new();

    public ComputedBuilder Define(string name, string expression, string returnType = "string")
    {
        _computed.Add(new ComputedDef
        {
            Name = name,
            Expression = expression,
            ReturnType = returnType
        });
        return this;
    }

    public List<ComputedDef> Build() => _computed;
}

/// <summary>
/// Context for template rendering (placeholder for future type-safe templates)
/// </summary>
public class RenderContext
{
    // Future: type-safe property accessors based on defined state/props
}

#region Data Models

public class ComponentDefinition
{
    public string Name { get; set; } = "";
    public bool IsClient { get; set; }
    public bool IsServer { get; set; }
    public List<FieldDef> StateFields { get; set; } = new();
    public List<PropDef> PropFields { get; set; } = new();
    public List<ActionDef> ActionDefs { get; set; } = new();
    public List<ComputedDef> ComputedFields { get; set; } = new();
    public List<ImportDef> Imports { get; set; } = new();
    public string? Template { get; set; }
    public Func<RenderContext, string>? TemplateFunc { get; set; }
}

public class FieldDef
{
    public string Name { get; set; } = "";
    public string TsType { get; set; } = "any";
    public string DefaultValue { get; set; } = "null";
}

public class PropDef
{
    public string Name { get; set; } = "";
    public string TsType { get; set; } = "any";
    public bool IsOptional { get; set; }
    public string? DefaultValue { get; set; }
}

public class ActionDef
{
    public string Name { get; set; } = "";
    public string? Parameters { get; set; }
    public string Mutation { get; set; } = "";
    public bool IsAsync { get; set; }
}

public class ComputedDef
{
    public string Name { get; set; } = "";
    public string Expression { get; set; } = "";
    public string ReturnType { get; set; } = "string";
}

public class ImportDef
{
    public string Path { get; set; } = "";
    public List<string>? NamedImports { get; set; }
}

#endregion

/// <summary>
/// Emits TypeScript/TSX from ComponentDefinition
/// </summary>
public class EnhancedEmitter
{
    public string Emit(ComponentDefinition def)
    {
        var sb = new System.Text.StringBuilder();

        // Imports
        sb.AppendLine("import React from 'react';");

        if (def.StateFields.Any())
        {
            sb.AppendLine("import { useStore } from '@tanstack/react-store';");
            sb.AppendLine("import { Store } from '@tanstack/store';");
        }

        foreach (var import in def.Imports)
        {
            if (import.NamedImports?.Any() == true)
            {
                sb.AppendLine($"import {{ {string.Join(", ", import.NamedImports)} }} from '{import.Path}';");
            }
            else
            {
                var name = Path.GetFileNameWithoutExtension(import.Path);
                sb.AppendLine($"import {{ {name} }} from '{import.Path}';");
            }
        }
        sb.AppendLine();

        // State interface and store
        if (def.StateFields.Any())
        {
            sb.AppendLine($"interface {def.Name}State {{");
            foreach (var field in def.StateFields)
            {
                sb.AppendLine($"  {field.Name}: {field.TsType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            var storeName = char.ToLower(def.Name[0]) + def.Name.Substring(1) + "Store";
            sb.AppendLine($"const {storeName} = new Store<{def.Name}State>({{");
            foreach (var field in def.StateFields)
            {
                sb.AppendLine($"  {field.Name}: {field.DefaultValue},");
            }
            sb.AppendLine("});");
            sb.AppendLine();
        }

        // Props interface
        if (def.PropFields.Any())
        {
            sb.AppendLine($"export interface {def.Name}Props {{");
            foreach (var prop in def.PropFields)
            {
                var opt = prop.IsOptional ? "?" : "";
                sb.AppendLine($"  {prop.Name}{opt}: {prop.TsType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Component function
        var propsParam = "";
        var propsDestructure = "";
        if (def.PropFields.Any())
        {
            var propNames = def.PropFields.Select(p =>
            {
                if (p.IsOptional && p.DefaultValue != null)
                    return $"{p.Name} = {p.DefaultValue}";
                return p.Name;
            });
            propsDestructure = string.Join(", ", propNames);
            propsParam = $"{{ {propsDestructure} }}: {def.Name}Props";
        }

        sb.AppendLine($"export function {def.Name}({propsParam}) {{");

        // State hooks
        if (def.StateFields.Any())
        {
            var storeName = char.ToLower(def.Name[0]) + def.Name.Substring(1) + "Store";
            foreach (var field in def.StateFields)
            {
                sb.AppendLine($"  const {field.Name} = useStore({storeName}, (s) => s.{field.Name});");
            }
            sb.AppendLine();

            // Auto-generate setters
            foreach (var field in def.StateFields)
            {
                var setter = "set" + char.ToUpper(field.Name[0]) + field.Name.Substring(1);
                sb.AppendLine($"  const {setter} = (value: {field.TsType}) => {storeName}.setState((s) => ({{ ...s, {field.Name}: value }}));");
            }
            sb.AppendLine();
        }

        // Computed values
        foreach (var comp in def.ComputedFields)
        {
            sb.AppendLine($"  const {comp.Name} = {TransformExpression(comp.Expression, def)};");
        }
        if (def.ComputedFields.Any()) sb.AppendLine();

        // Actions
        foreach (var action in def.ActionDefs)
        {
            var asyncPrefix = action.IsAsync ? "async " : "";
            var actionParams = action.Parameters ?? "";
            var body = TransformMutation(action.Mutation, def);
            sb.AppendLine($"  const {action.Name} = {asyncPrefix}({actionParams}) => {{");
            sb.AppendLine($"    {body}");
            sb.AppendLine("  };");
            sb.AppendLine();
        }

        // Return JSX
        sb.AppendLine("  return (");
        var template = def.Template ?? GenerateDefaultTemplate(def);
        foreach (var line in template.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                sb.AppendLine($"    {line.Trim()}");
        }
        sb.AppendLine("  );");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string TransformExpression(string expr, ComponentDefinition def)
    {
        // Convert C# string interpolation to JS template literal
        if (expr.StartsWith("$\"") && expr.EndsWith("\""))
        {
            var inner = expr.Substring(2, expr.Length - 3);
            inner = System.Text.RegularExpressions.Regex.Replace(inner, @"\{([^}]+)\}", m => "${" + m.Groups[1].Value + "}");
            return $"`{inner}`";
        }
        return expr;
    }

    private string TransformMutation(string mutation, ComponentDefinition def)
    {
        var result = mutation;
        var storeName = char.ToLower(def.Name[0]) + def.Name.Substring(1) + "Store";

        foreach (var field in def.StateFields)
        {
            // field++
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{field.Name}\+\+",
                $"{storeName}.setState((s) => ({{ ...s, {field.Name}: s.{field.Name} + 1 }}))");

            // field--
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{field.Name}--",
                $"{storeName}.setState((s) => ({{ ...s, {field.Name}: s.{field.Name} - 1 }}))");

            // field += value
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{field.Name}\s*\+=\s*([^;]+);?",
                m => $"{storeName}.setState((s) => ({{ ...s, {field.Name}: s.{field.Name} + {m.Groups[1].Value.Trim()} }}));");

            // field -= value
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{field.Name}\s*-=\s*([^;]+);?",
                m => $"{storeName}.setState((s) => ({{ ...s, {field.Name}: s.{field.Name} - {m.Groups[1].Value.Trim()} }}));");

            // field = value
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{field.Name}\s*=\s*([^;]+);",
                m => $"{storeName}.setState((s) => ({{ ...s, {field.Name}: {m.Groups[1].Value.Trim()} }}));");
        }

        return result.Trim();
    }

    private string GenerateDefaultTemplate(ComponentDefinition def)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<div>");
        foreach (var field in def.StateFields)
        {
            sb.AppendLine($"  <span>{{{field.Name}}}</span>");
        }
        sb.AppendLine("</div>");
        return sb.ToString();
    }
}

namespace Shalimar.Razor;

/// <summary>
/// Fluent DSL for building React components with TanStack Store.
/// Produces ComponentInfo that feeds into TsxEmitter.
/// </summary>
public class ComponentBuilder
{
    private readonly ComponentInfo _info;

    private ComponentBuilder(string name)
    {
        _info = new ComponentInfo
        {
            Name = name,
            Props = new List<PropInfo>(),
            StoreVars = new List<StoreVarInfo>(),
            Methods = new List<MethodInfo>(),
            Imports = new List<ImportInfo>(),
            Directive = ComponentDirective.Default
        };
    }

    /// <summary>
    /// Create a new component builder
    /// </summary>
    public static ComponentBuilder Create(string name) => new(name);

    /// <summary>
    /// Mark as client component (uses TanStack Store)
    /// </summary>
    public ComponentBuilder Client()
    {
        _info.Directive = ComponentDirective.Client;
        return this;
    }

    /// <summary>
    /// Mark as server component
    /// </summary>
    public ComponentBuilder Server()
    {
        _info.Directive = ComponentDirective.Server;
        return this;
    }

    /// <summary>
    /// Add a store state variable
    /// </summary>
    public ComponentBuilder Store<T>(string name, T defaultValue)
    {
        _info.StoreVars.Add(new StoreVarInfo
        {
            Name = name,
            CSharpType = typeof(T).Name.ToLower(),
            TypeScriptType = ToTsType<T>(),
            DefaultValue = ToTsValue(defaultValue)
        });
        return this;
    }

    /// <summary>
    /// Add a required prop
    /// </summary>
    public ComponentBuilder Prop<T>(string name)
    {
        _info.Props.Add(new PropInfo
        {
            Name = name,
            CSharpType = typeof(T).Name,
            TypeScriptType = ToTsType<T>(),
            DefaultValue = null
        });
        return this;
    }

    /// <summary>
    /// Add an optional prop with default value
    /// </summary>
    public ComponentBuilder Prop<T>(string name, T defaultValue)
    {
        _info.Props.Add(new PropInfo
        {
            Name = name,
            CSharpType = typeof(T).Name,
            TypeScriptType = ToTsType<T>(),
            DefaultValue = ToTsValue(defaultValue)
        });
        return this;
    }

    /// <summary>
    /// Add a method/action (body as string for now)
    /// </summary>
    public ComponentBuilder Action(string name, string body, bool isAsync = false)
    {
        _info.Methods.Add(new MethodInfo
        {
            Name = name,
            Body = body,
            IsAsync = isAsync
        });
        return this;
    }

    /// <summary>
    /// Add an import
    /// </summary>
    public ComponentBuilder Import(string path, params string[] names)
    {
        if (names.Length == 0)
        {
            _info.Imports.Add(new ImportInfo
            {
                Path = path,
                IsDefault = true
            });
        }
        else
        {
            _info.Imports.Add(new ImportInfo
            {
                Path = path,
                NamedImports = names,
                IsDefault = false
            });
        }
        return this;
    }

    /// <summary>
    /// Build the component info
    /// </summary>
    public ComponentInfo Build() => _info;

    /// <summary>
    /// Generate TSX directly
    /// </summary>
    public string ToTsx(string? template = null)
    {
        var emitter = new ComponentEmitter();
        return emitter.Emit(_info, template);
    }

    private static string ToTsType<T>()
    {
        var type = typeof(T);
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(DateTime)) return "Date";
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            var innerType = type.GetGenericArguments()[0];
            return $"{ToTsTypeFromType(innerType)}[]";
        }
        return type.Name;
    }

    private static string ToTsTypeFromType(Type type)
    {
        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float)) return "number";
        if (type == typeof(bool)) return "boolean";
        return type.Name;
    }

    private static string ToTsValue<T>(T value)
    {
        if (value == null) return "null";
        if (value is string s) return s == "" ? "\"\"" : $"\"{s}\"";
        if (value is bool b) return b ? "true" : "false";
        if (value is int or long or double or float) return value.ToString()!;
        return value.ToString()!;
    }
}

/// <summary>
/// Emits TSX from ComponentInfo (separated from Razor parsing)
/// </summary>
public class ComponentEmitter
{
    public string Emit(ComponentInfo component, string? template = null)
    {
        var sb = new System.Text.StringBuilder();

        // Imports
        sb.AppendLine("import React from 'react';");

        if (component.StoreVars.Any())
        {
            sb.AppendLine("import { useStore } from '@tanstack/react-store';");
            sb.AppendLine("import { Store } from '@tanstack/store';");
        }

        foreach (var import in component.Imports)
        {
            if (import.IsDefault)
            {
                var name = import.Alias ?? Path.GetFileNameWithoutExtension(import.Path);
                sb.AppendLine($"import {{ {name} }} from '{import.Path}';");
            }
            else if (import.NamedImports != null)
            {
                sb.AppendLine($"import {{ {string.Join(", ", import.NamedImports)} }} from '{import.Path}';");
            }
        }
        sb.AppendLine();

        // Store state interface and instance
        if (component.StoreVars.Any())
        {
            sb.AppendLine($"interface {component.Name}State {{");
            foreach (var v in component.StoreVars)
            {
                sb.AppendLine($"  {v.Name}: {v.TypeScriptType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            var storeName = char.ToLower(component.Name[0]) + component.Name.Substring(1) + "Store";
            sb.AppendLine($"const {storeName} = new Store<{component.Name}State>({{");
            foreach (var v in component.StoreVars)
            {
                sb.AppendLine($"  {v.Name}: {v.DefaultValue},");
            }
            sb.AppendLine("});");
            sb.AppendLine();
        }

        // Props interface
        if (component.Props.Any())
        {
            sb.AppendLine($"export interface {component.Name}Props {{");
            foreach (var prop in component.Props)
            {
                var opt = prop.DefaultValue != null ? "?" : "";
                sb.AppendLine($"  {prop.Name}{opt}: {prop.TypeScriptType};");
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Component function
        var propsParam = component.Props.Any()
            ? $"{{ {string.Join(", ", component.Props.Select(p => p.Name))} }}: {component.Name}Props"
            : "";

        sb.AppendLine($"export function {component.Name}({propsParam}) {{");

        // Store hooks
        if (component.StoreVars.Any())
        {
            var storeName = char.ToLower(component.Name[0]) + component.Name.Substring(1) + "Store";
            foreach (var v in component.StoreVars)
            {
                sb.AppendLine($"  const {v.Name} = useStore({storeName}, (s) => s.{v.Name});");
            }
            sb.AppendLine();

            // Setters
            foreach (var v in component.StoreVars)
            {
                var setter = "set" + char.ToUpper(v.Name[0]) + v.Name.Substring(1);
                sb.AppendLine($"  const {setter} = (value: {v.TypeScriptType}) => {storeName}.setState((s) => ({{ ...s, {v.Name}: value }}));");
            }
            sb.AppendLine();
        }

        // Methods
        foreach (var method in component.Methods)
        {
            var asyncPrefix = method.IsAsync ? "async " : "";
            var body = TransformMethodBody(method.Body, component);
            sb.AppendLine($"  const {method.Name} = {asyncPrefix}() => {{");
            sb.AppendLine($"    {body}");
            sb.AppendLine("  };");
            sb.AppendLine();
        }

        // Return JSX
        sb.AppendLine("  return (");
        if (!string.IsNullOrEmpty(template))
        {
            sb.AppendLine($"    {template}");
        }
        else
        {
            // Auto-generate minimal template
            sb.AppendLine("    <div>");
            foreach (var v in component.StoreVars)
            {
                sb.AppendLine($"      <span>{{{v.Name}}}</span>");
            }
            sb.AppendLine("    </div>");
        }
        sb.AppendLine("  );");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string TransformMethodBody(string body, ComponentInfo component)
    {
        var result = body;
        var storeName = char.ToLower(component.Name[0]) + component.Name.Substring(1) + "Store";

        foreach (var v in component.StoreVars)
        {
            // count++
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{v.Name}\+\+",
                $"{storeName}.setState((s) => ({{ ...s, {v.Name}: s.{v.Name} + 1 }}))");

            // count--
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{v.Name}--",
                $"{storeName}.setState((s) => ({{ ...s, {v.Name}: s.{v.Name} - 1 }}))");

            // count = value
            result = System.Text.RegularExpressions.Regex.Replace(
                result, $@"\b{v.Name}\s*=\s*([^;]+);",
                m => $"{storeName}.setState((s) => ({{ ...s, {v.Name}: {m.Groups[1].Value.Trim()} }}));");
        }

        return result;
    }
}

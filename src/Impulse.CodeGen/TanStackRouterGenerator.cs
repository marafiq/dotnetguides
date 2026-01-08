using System.Text;
using System.Text.RegularExpressions;
using Impulse.Core;

namespace Impulse.CodeGen;

/// <summary>
/// Generates TanStack Router configuration from Minimal API routes.
/// The server defines routes → this generates the client router.
/// </summary>
public sealed partial class TanStackRouterGenerator
{
    /// <summary>
    /// Generates router.g.ts with full TanStack Router setup.
    /// </summary>
    public string Generate(ImpulseTypeRegistry registry, string version = "1.0.0")
    {
        var sb = new StringBuilder();

        sb.AppendLine("// This file is auto-generated from Minimal API routes");
        sb.AppendLine("// Do not edit manually - regenerate with: dotnet impulse codegen");
        sb.AppendLine();
        sb.AppendLine("import {");
        sb.AppendLine("  createRouter,");
        sb.AppendLine("  createRoute,");
        sb.AppendLine("  createRootRoute,");
        sb.AppendLine("  Outlet,");
        sb.AppendLine("} from '@tanstack/react-router';");
        sb.AppendLine("import { createElement } from 'react';");
        sb.AppendLine();

        // Import types
        sb.AppendLine("// Generated types from server");
        var propsTypes = registry.PropsTypes.ToList();
        if (propsTypes.Count > 0)
        {
            sb.Append("import type { ");
            sb.Append(string.Join(", ", propsTypes.Select(t => t.Name)));
            sb.AppendLine(" } from './types.g';");
        }
        sb.AppendLine();

        // Import components from registry
        sb.AppendLine("// Components from registry");
        sb.AppendLine("import { IMPULSE_COMPONENTS } from './registry.g';");
        sb.AppendLine();

        // Impulse headers
        sb.AppendLine("// Impulse protocol headers");
        sb.AppendLine("const IMPULSE_HEADERS = {");
        sb.AppendLine("  Impulse: 'X-Impulse',");
        sb.AppendLine("  Version: 'X-Impulse-Version',");
        sb.AppendLine("  Reload: 'X-Impulse-Reload',");
        sb.AppendLine("};");
        sb.AppendLine();

        // Version constant
        sb.AppendLine($"const IMPULSE_VERSION = '{version}';");
        sb.AppendLine();

        // Generic loader function
        sb.AppendLine("// Generic loader for Impulse routes");
        sb.AppendLine("async function impulseLoader<T>(url: string): Promise<T> {");
        sb.AppendLine("  const response = await fetch(url, {");
        sb.AppendLine("    headers: {");
        sb.AppendLine("      [IMPULSE_HEADERS.Impulse]: 'true',");
        sb.AppendLine("      [IMPULSE_HEADERS.Version]: IMPULSE_VERSION,");
        sb.AppendLine("      Accept: 'application/json',");
        sb.AppendLine("    },");
        sb.AppendLine("  });");
        sb.AppendLine();
        sb.AppendLine("  if (response.headers.get(IMPULSE_HEADERS.Reload) === 'true') {");
        sb.AppendLine("    window.location.reload();");
        sb.AppendLine("    return new Promise(() => {});");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  if (!response.ok) {");
        sb.AppendLine("    throw new Error(`Failed to load ${url}: ${response.status}`);");
        sb.AppendLine("  }");
        sb.AppendLine();
        sb.AppendLine("  const data = await response.json();");
        sb.AppendLine("  return data.props;");
        sb.AppendLine("}");
        sb.AppendLine();

        // Root route
        sb.AppendLine("// Root route");
        sb.AppendLine("const rootRoute = createRootRoute({");
        sb.AppendLine("  component: () => createElement(Outlet),");
        sb.AppendLine("});");
        sb.AppendLine();

        // Generate routes from registry
        sb.AppendLine("// Routes generated from Minimal API");
        var routes = registry.Components
            .OrderBy(c => c.Key)
            .ToList();

        var routeNames = new List<string>();

        foreach (var (path, metadata) in routes)
        {
            var routeName = GenerateRouteName(path);
            routeNames.Add(routeName);

            var componentPath = metadata.ComponentPath;
            var propsType = metadata.PropsType?.Name ?? "unknown";
            var tanstackPath = ConvertToTanStackPath(path);

            sb.AppendLine($"const {routeName} = createRoute({{");
            sb.AppendLine($"  getParentRoute: () => rootRoute,");
            sb.AppendLine($"  path: '{tanstackPath}',");
            sb.AppendLine($"  loader: async ({{ params }}) => {{");
            sb.AppendLine($"    const url = buildUrl('{path}', params);");
            sb.AppendLine($"    return impulseLoader<{propsType}>(url);");
            sb.AppendLine($"  }},");
            sb.AppendLine($"  component: function {ToPascalCase(routeName)}() {{");
            sb.AppendLine($"    const data = {routeName}.useLoaderData();");
            sb.AppendLine($"    const Component = IMPULSE_COMPONENTS.get('{componentPath}');");
            sb.AppendLine($"    if (!Component) return createElement('div', null, 'Component not found: {componentPath}');");
            sb.AppendLine($"    return createElement(Component, data as object);");
            sb.AppendLine($"  }},");
            sb.AppendLine($"}});");
            sb.AppendLine();
        }

        // URL builder helper
        sb.AppendLine("// URL builder for parameterized routes");
        sb.AppendLine("function buildUrl(template: string, params: Record<string, string>): string {");
        sb.AppendLine("  let url = template;");
        sb.AppendLine("  for (const [key, value] of Object.entries(params)) {");
        sb.AppendLine("    url = url.replace(`{${key}}`, value);");
        sb.AppendLine("    url = url.replace(new RegExp(`\\\\{${key}:[^}]+\\\\}`, 'g'), value);");
        sb.AppendLine("  }");
        sb.AppendLine("  return url;");
        sb.AppendLine("}");
        sb.AppendLine();

        // Route tree
        sb.AppendLine("// Route tree");
        sb.AppendLine("const routeTree = rootRoute.addChildren([");
        foreach (var name in routeNames)
        {
            sb.AppendLine($"  {name},");
        }
        sb.AppendLine("]);");
        sb.AppendLine();

        // Create router
        sb.AppendLine("// Create the router instance");
        sb.AppendLine("export const router = createRouter({");
        sb.AppendLine("  routeTree,");
        sb.AppendLine("  defaultPreload: 'intent',");
        sb.AppendLine("});");
        sb.AppendLine();

        // Type declarations for TanStack Router
        sb.AppendLine("// Type declarations for TanStack Router");
        sb.AppendLine("declare module '@tanstack/react-router' {");
        sb.AppendLine("  interface Register {");
        sb.AppendLine("    router: typeof router;");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();

        // Export route paths for type-safe navigation
        sb.AppendLine("// Type-safe route paths");
        sb.AppendLine("export const Routes = {");
        foreach (var (path, _) in routes)
        {
            var funcName = GenerateRouteHelperName(path);
            var (signature, body) = GenerateRouteFunction(path);
            sb.AppendLine($"  {funcName}: {signature} => {body},");
        }
        sb.AppendLine("} as const;");

        return sb.ToString();
    }

    private static string GenerateRouteName(string path)
    {
        // /residents/{id} → residentsIdRoute
        var clean = path.Trim('/').Replace("/", "_").Replace("{", "").Replace("}", "").Replace(":", "_");
        if (string.IsNullOrEmpty(clean)) clean = "index";
        return ToCamelCase(clean) + "Route";
    }

    private static string GenerateRouteHelperName(string path)
    {
        var parts = path.Trim('/').Split('/')
            .Where(p => !ParamPattern().IsMatch(p))
            .ToList();

        if (parts.Count == 0) return "index";
        return ToCamelCase(string.Join("_", parts));
    }

    private static string ConvertToTanStackPath(string path)
    {
        // /residents/{id:int} → /residents/$id
        var result = ParamPattern().Replace(path, m => $"${m.Groups[1].Value}");
        return result;
    }

    private static (string signature, string body) GenerateRouteFunction(string path)
    {
        var matches = ParamPattern().Matches(path);

        if (matches.Count == 0)
        {
            return ("()", $"'{path}' as const");
        }

        var parameters = new List<string>();
        var template = path;

        foreach (Match match in matches)
        {
            var paramName = match.Groups[1].Value;
            var paramType = match.Groups[2].Success ? GetTypeScriptType(match.Groups[2].Value) : "string";
            parameters.Add($"{ToCamelCase(paramName)}: {paramType}");
            template = template.Replace(match.Value, $"${{{ToCamelCase(paramName)}}}");
        }

        return ($"({string.Join(", ", parameters)})", $"`{template}` as const");
    }

    private static string GetTypeScriptType(string constraint) => constraint switch
    {
        "int" or "long" => "number",
        "guid" => "string",
        "bool" => "boolean",
        _ => "string"
    };

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var pascal = ToPascalCase(name);
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var parts = name.Split('_', '-');
        return string.Concat(parts.Select(p =>
            string.IsNullOrEmpty(p) ? "" : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }

    [GeneratedRegex(@"\{(\w+)(?::(\w+))?\}")]
    private static partial Regex ParamPattern();
}

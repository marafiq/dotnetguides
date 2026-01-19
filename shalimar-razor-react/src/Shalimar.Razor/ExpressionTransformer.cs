using System.Text.RegularExpressions;

namespace Shalimar.Razor;

/// <summary>
/// Transforms C# expressions to JavaScript/TypeScript equivalents.
///
/// Key transformations:
/// - Props.X → props.X (lowercase props)
/// - list.Select(x => ...) → list.map(x => ...)
/// - list.Where(x => ...) → list.filter(x => ...)
/// - list.Any(x => ...) → list.some(x => ...)
/// - list.All(x => ...) → list.every(x => ...)
/// - list.Count → list.length
/// - str.Length → str.length
/// - str.ToUpper() → str.toUpperCase()
/// - string.IsNullOrEmpty(s) → !s
/// </summary>
public static class ExpressionTransformer
{
    /// <summary>
    /// Transform a C# expression to JavaScript.
    /// </summary>
    public static string Transform(string csharpExpr)
    {
        if (string.IsNullOrWhiteSpace(csharpExpr))
        {
            return csharpExpr;
        }

        var result = csharpExpr.Trim();

        // Remove leading @ if present
        if (result.StartsWith("@"))
        {
            result = result.Substring(1);
        }

        // Remove explicit expression parentheses @(...)
        if (result.StartsWith("(") && result.EndsWith(")"))
        {
            var inner = result.Substring(1, result.Length - 2);
            // Only unwrap if it's a simple expression wrapper, not a tuple or grouped expression
            if (!inner.Contains(",") && BalancedParens(inner))
            {
                result = inner;
            }
        }

        // Transform Props → props
        result = Regex.Replace(result, @"\bProps\.", "props.");

        // Transform LINQ methods to JavaScript array methods
        result = TransformLinqMethods(result);

        // Transform string methods
        result = TransformStringMethods(result);

        // Transform common C# patterns
        result = TransformCommonPatterns(result);

        // Transform lambda expressions
        result = TransformLambdas(result);

        return result;
    }

    private static string TransformLinqMethods(string expr)
    {
        // Select → map
        expr = Regex.Replace(expr, @"\.Select\s*\(", ".map(");

        // Where → filter
        expr = Regex.Replace(expr, @"\.Where\s*\(", ".filter(");

        // Any → some (with predicate)
        expr = Regex.Replace(expr, @"\.Any\s*\(", ".some(");

        // Any() → length > 0 (without predicate)
        expr = Regex.Replace(expr, @"\.Any\s*\(\s*\)", ".length > 0");

        // All → every
        expr = Regex.Replace(expr, @"\.All\s*\(", ".every(");

        // First() → [0]
        expr = Regex.Replace(expr, @"\.First\s*\(\s*\)", "[0]");

        // FirstOrDefault() → [0] ?? null (simplified)
        expr = Regex.Replace(expr, @"\.FirstOrDefault\s*\(\s*\)", "[0]");

        // Last() → slice(-1)[0]
        expr = Regex.Replace(expr, @"\.Last\s*\(\s*\)", ".slice(-1)[0]");

        // LastOrDefault() → slice(-1)[0] ?? null
        expr = Regex.Replace(expr, @"\.LastOrDefault\s*\(\s*\)", ".slice(-1)[0]");

        // Count → length
        expr = Regex.Replace(expr, @"\.Count(?!\()", ".length");

        // Count() → length
        expr = Regex.Replace(expr, @"\.Count\s*\(\s*\)", ".length");

        // Take(n) → slice(0, n)
        expr = Regex.Replace(expr, @"\.Take\s*\(\s*(\d+)\s*\)", ".slice(0, $1)");

        // Skip(n) → slice(n)
        expr = Regex.Replace(expr, @"\.Skip\s*\(\s*(\d+)\s*\)", ".slice($1)");

        // OrderBy → sort (simplified, doesn't handle complex selectors)
        expr = Regex.Replace(expr, @"\.OrderBy\s*\([^)]+\)", ".sort()");

        // OrderByDescending → sort().reverse()
        expr = Regex.Replace(expr, @"\.OrderByDescending\s*\([^)]+\)", ".sort().reverse()");

        // Contains → includes
        expr = Regex.Replace(expr, @"\.Contains\s*\(", ".includes(");

        // Distinct → [...new Set()]
        expr = Regex.Replace(expr, @"(\w+)\.Distinct\s*\(\s*\)", "[...new Set($1)]");

        // ToList() / ToArray() → no-op in JS
        expr = Regex.Replace(expr, @"\.ToList\s*\(\s*\)", "");
        expr = Regex.Replace(expr, @"\.ToArray\s*\(\s*\)", "");

        return expr;
    }

    private static string TransformStringMethods(string expr)
    {
        // Length → length
        expr = Regex.Replace(expr, @"\.Length(?!\()", ".length");

        // ToUpper() → toUpperCase()
        expr = Regex.Replace(expr, @"\.ToUpper\s*\(\s*\)", ".toUpperCase()");

        // ToLower() → toLowerCase()
        expr = Regex.Replace(expr, @"\.ToLower\s*\(\s*\)", ".toLowerCase()");

        // ToUpperInvariant() → toUpperCase()
        expr = Regex.Replace(expr, @"\.ToUpperInvariant\s*\(\s*\)", ".toUpperCase()");

        // ToLowerInvariant() → toLowerCase()
        expr = Regex.Replace(expr, @"\.ToLowerInvariant\s*\(\s*\)", ".toLowerCase()");

        // Trim() → trim()
        expr = Regex.Replace(expr, @"\.Trim\s*\(\s*\)", ".trim()");

        // TrimStart() → trimStart()
        expr = Regex.Replace(expr, @"\.TrimStart\s*\(\s*\)", ".trimStart()");

        // TrimEnd() → trimEnd()
        expr = Regex.Replace(expr, @"\.TrimEnd\s*\(\s*\)", ".trimEnd()");

        // StartsWith → startsWith
        expr = Regex.Replace(expr, @"\.StartsWith\s*\(", ".startsWith(");

        // EndsWith → endsWith
        expr = Regex.Replace(expr, @"\.EndsWith\s*\(", ".endsWith(");

        // Substring(start, length) → substring(start, start + length) - simplified
        expr = Regex.Replace(expr, @"\.Substring\s*\(", ".substring(");

        // Replace → replace
        expr = Regex.Replace(expr, @"\.Replace\s*\(", ".replace(");

        // Split → split
        expr = Regex.Replace(expr, @"\.Split\s*\(", ".split(");

        // Join (static) → join (instance)
        expr = Regex.Replace(expr, @"string\.Join\s*\(\s*([^,]+),\s*([^)]+)\)", "$2.join($1)");

        // string.IsNullOrEmpty(s) → !s
        expr = Regex.Replace(expr, @"string\.IsNullOrEmpty\s*\(\s*([^)]+)\s*\)", "!$1");

        // string.IsNullOrWhiteSpace(s) → !s?.trim()
        expr = Regex.Replace(expr, @"string\.IsNullOrWhiteSpace\s*\(\s*([^)]+)\s*\)", "!$1?.trim()");

        // IndexOf → indexOf
        expr = Regex.Replace(expr, @"\.IndexOf\s*\(", ".indexOf(");

        // PadLeft → padStart
        expr = Regex.Replace(expr, @"\.PadLeft\s*\(", ".padStart(");

        // PadRight → padEnd
        expr = Regex.Replace(expr, @"\.PadRight\s*\(", ".padEnd(");

        return expr;
    }

    private static string TransformCommonPatterns(string expr)
    {
        // Handle null-conditional and null-coalescing (same in JS/TS)
        // x?.Property stays as x?.Property
        // x ?? default stays as x ?? default

        // Boolean literals
        expr = Regex.Replace(expr, @"\btrue\b", "true");
        expr = Regex.Replace(expr, @"\bfalse\b", "false");
        expr = Regex.Replace(expr, @"\bnull\b", "null");

        // typeof → typeof (same)
        // nameof → string literal (simplified - just remove nameof)
        expr = Regex.Replace(expr, @"nameof\s*\(\s*([^)]+)\s*\)", "\"$1\"");

        // new List<T> { ... } → [...]
        expr = Regex.Replace(expr, @"new\s+List<[^>]+>\s*\{([^}]*)\}", "[$1]");

        // new[] { ... } → [...]
        expr = Regex.Replace(expr, @"new\s*\[\]\s*\{([^}]*)\}", "[$1]");

        // DateTime.Now → new Date()
        expr = Regex.Replace(expr, @"DateTime\.Now", "new Date()");
        expr = Regex.Replace(expr, @"DateTime\.Today", "new Date()");
        expr = Regex.Replace(expr, @"DateTime\.UtcNow", "new Date()");

        // Math methods (same names in JS)
        expr = Regex.Replace(expr, @"Math\.Abs\s*\(", "Math.abs(");
        expr = Regex.Replace(expr, @"Math\.Floor\s*\(", "Math.floor(");
        expr = Regex.Replace(expr, @"Math\.Ceiling\s*\(", "Math.ceil(");
        expr = Regex.Replace(expr, @"Math\.Round\s*\(", "Math.round(");
        expr = Regex.Replace(expr, @"Math\.Max\s*\(", "Math.max(");
        expr = Regex.Replace(expr, @"Math\.Min\s*\(", "Math.min(");
        expr = Regex.Replace(expr, @"Math\.Pow\s*\(", "Math.pow(");
        expr = Regex.Replace(expr, @"Math\.Sqrt\s*\(", "Math.sqrt(");

        // int.Parse → parseInt
        expr = Regex.Replace(expr, @"int\.Parse\s*\(", "parseInt(");

        // double.Parse / float.Parse → parseFloat
        expr = Regex.Replace(expr, @"double\.Parse\s*\(", "parseFloat(");
        expr = Regex.Replace(expr, @"float\.Parse\s*\(", "parseFloat(");

        // .ToString() → .toString()
        expr = Regex.Replace(expr, @"\.ToString\s*\(\s*\)", ".toString()");

        // Guid.NewGuid().ToString() → crypto.randomUUID()
        expr = Regex.Replace(expr, @"Guid\.NewGuid\s*\(\s*\)\.ToString\s*\(\s*\)", "crypto.randomUUID()");

        return expr;
    }

    private static string TransformLambdas(string expr)
    {
        // C# lambda (x) => expr → (x) => expr (same in JS)
        // The syntax is compatible, but we might need to transform the body

        // Handle single-parameter lambdas without parens: x => expr
        // This is already valid JS syntax

        // Handle expression-bodied lambdas: (x, y) => x + y
        // This is already valid JS syntax

        return expr;
    }

    private static bool BalancedParens(string s)
    {
        int depth = 0;
        foreach (char c in s)
        {
            if (c == '(') depth++;
            else if (c == ')') depth--;
            if (depth < 0) return false;
        }
        return depth == 0;
    }
}

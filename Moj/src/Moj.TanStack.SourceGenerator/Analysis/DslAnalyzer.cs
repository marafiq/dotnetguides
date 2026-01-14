using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Moj.TanStack.SourceGenerator;

/// <summary>
/// Analyzes DSL method chains to extract TypeScript generation information
/// </summary>
internal sealed class DslAnalyzer
{
    private readonly SemanticModel _semanticModel;

    public DslAnalyzer(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public IEnumerable<string> AnalyzeClass(ClassDeclarationSyntax classDeclaration)
    {
        var results = new List<string>();

        // Find all method declarations that return DSL types
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method)
            {
                var result = AnalyzeMethod(method);
                if (!string.IsNullOrEmpty(result))
                    results.Add(result);
            }
            else if (member is PropertyDeclarationSyntax property)
            {
                var result = AnalyzeProperty(property);
                if (!string.IsNullOrEmpty(result))
                    results.Add(result);
            }
            else if (member is FieldDeclarationSyntax field)
            {
                var result = AnalyzeField(field);
                if (!string.IsNullOrEmpty(result))
                    results.Add(result);
            }
        }

        return results;
    }

    private string? AnalyzeMethod(MethodDeclarationSyntax method)
    {
        if (method.Body == null && method.ExpressionBody == null)
            return null;

        var expression = method.ExpressionBody?.Expression;
        if (expression == null && method.Body?.Statements.Count > 0)
        {
            // Look for return statement
            var returnStatement = method.Body.Statements
                .OfType<ReturnStatementSyntax>()
                .FirstOrDefault();
            expression = returnStatement?.Expression;
        }

        if (expression == null)
            return null;

        return AnalyzeExpression(expression, method.Identifier.Text);
    }

    private string? AnalyzeProperty(PropertyDeclarationSyntax property)
    {
        var expression = property.ExpressionBody?.Expression;
        if (expression == null && property.Initializer != null)
        {
            expression = property.Initializer.Value;
        }

        if (expression == null)
            return null;

        return AnalyzeExpression(expression, property.Identifier.Text);
    }

    private string? AnalyzeField(FieldDeclarationSyntax field)
    {
        foreach (var variable in field.Declaration.Variables)
        {
            if (variable.Initializer?.Value != null)
            {
                var result = AnalyzeExpression(variable.Initializer.Value, variable.Identifier.Text);
                if (result != null)
                    return result;
            }
        }
        return null;
    }

    private string? AnalyzeExpression(ExpressionSyntax expression, string variableName)
    {
        // Analyze method chains like Ts.Const<T>("name").Value(...).Build()
        var chain = ExtractMethodChain(expression);
        if (chain.Count == 0)
            return null;

        var sb = new StringBuilder();

        // Determine what kind of declaration this is
        var firstCall = chain[0];
        var declarationType = DetermineDeclarationType(firstCall);

        switch (declarationType)
        {
            case DeclType.Const:
            case DeclType.Let:
                return GenerateVariableDeclaration(chain, variableName, declarationType);

            case DeclType.Store:
                return GenerateStoreDeclaration(chain, variableName);

            case DeclType.Query:
                return GenerateQueryDeclaration(chain, variableName);

            case DeclType.Route:
                return GenerateRouteDeclaration(chain, variableName);

            case DeclType.Router:
                return GenerateRouterDeclaration(chain, variableName);

            case DeclType.Interface:
                return GenerateInterfaceDeclaration(chain, variableName);

            case DeclType.Function:
                return GenerateFunctionDeclaration(chain, variableName);

            case DeclType.Object:
                return GenerateObjectExpression(chain, variableName);

            case DeclType.Arrow:
                return GenerateArrowFunction(chain, variableName);

            default:
                return GenerateGenericExpression(chain, variableName);
        }
    }

    private List<InvocationInfo> ExtractMethodChain(ExpressionSyntax expression)
    {
        var chain = new List<InvocationInfo>();
        ExtractMethodChainRecursive(expression, chain);
        chain.Reverse(); // Get in order from first to last call
        return chain;
    }

    private void ExtractMethodChainRecursive(ExpressionSyntax expression, List<InvocationInfo> chain)
    {
        switch (expression)
        {
            case InvocationExpressionSyntax invocation:
                var info = new InvocationInfo
                {
                    MethodName = GetMethodName(invocation.Expression),
                    Arguments = invocation.ArgumentList.Arguments.ToList(),
                    TypeArguments = GetTypeArguments(invocation.Expression)
                };
                chain.Add(info);

                // Continue up the chain
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    ExtractMethodChainRecursive(memberAccess.Expression, chain);
                }
                break;

            case MemberAccessExpressionSyntax member:
                // Static member access like Ts.Const
                if (member.Expression is IdentifierNameSyntax identifier)
                {
                    chain.Add(new InvocationInfo
                    {
                        MethodName = $"{identifier.Identifier.Text}.{member.Name.Identifier.Text}",
                        Arguments = new List<ArgumentSyntax>(),
                        TypeArguments = GetTypeArguments(member.Name)
                    });
                }
                break;

            case ObjectCreationExpressionSyntax objectCreation:
                chain.Add(new InvocationInfo
                {
                    MethodName = "new " + objectCreation.Type.ToString(),
                    Arguments = objectCreation.ArgumentList?.Arguments.ToList() ?? new List<ArgumentSyntax>(),
                    TypeArguments = new List<TypeSyntax>()
                });
                break;
        }
    }

    private static string GetMethodName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => expression.ToString()
        };
    }

    private static List<TypeSyntax> GetTypeArguments(ExpressionSyntax expression)
    {
        return expression switch
        {
            GenericNameSyntax generic => generic.TypeArgumentList.Arguments.ToList(),
            MemberAccessExpressionSyntax { Name: GenericNameSyntax genericName } =>
                genericName.TypeArgumentList.Arguments.ToList(),
            _ => new List<TypeSyntax>()
        };
    }

    private static DeclType DetermineDeclarationType(InvocationInfo firstCall)
    {
        var methodName = firstCall.MethodName.ToLowerInvariant();

        if (methodName.Contains("ts.const") || methodName == "const")
            return DeclType.Const;
        if (methodName.Contains("ts.let") || methodName == "let")
            return DeclType.Let;
        if (methodName.Contains("createstore") || methodName.Contains("store"))
            return DeclType.Store;
        if (methodName.Contains("queryoptions") || methodName.Contains("query"))
            return DeclType.Query;
        if (methodName.Contains("createroute") || methodName.Contains("route"))
            return DeclType.Route;
        if (methodName.Contains("createrouter") || methodName.Contains("router"))
            return DeclType.Router;
        if (methodName.Contains("interface"))
            return DeclType.Interface;
        if (methodName.Contains("function"))
            return DeclType.Function;
        if (methodName.Contains("object") || methodName.Contains("ts.object"))
            return DeclType.Object;
        if (methodName.Contains("arrow") || methodName.Contains("ts.arrow"))
            return DeclType.Arrow;

        return DeclType.Unknown;
    }

    private string GenerateVariableDeclaration(List<InvocationInfo> chain, string variableName, DeclType declType)
    {
        var keyword = declType == DeclType.Const ? "const" : "let";
        var hasExport = chain.Any(c => c.MethodName.Equals("Export", StringComparison.OrdinalIgnoreCase));
        var exportKeyword = hasExport ? "export " : "";

        // Extract the declared name and value
        var nameCall = chain.FirstOrDefault(c =>
            c.MethodName.Equals("Const", StringComparison.OrdinalIgnoreCase) ||
            c.MethodName.Equals("Let", StringComparison.OrdinalIgnoreCase) ||
            c.MethodName.StartsWith("Ts.Const", StringComparison.OrdinalIgnoreCase) ||
            c.MethodName.StartsWith("Ts.Let", StringComparison.OrdinalIgnoreCase));

        var declaredName = variableName;
        if (nameCall?.Arguments.Count > 0)
        {
            declaredName = ExtractStringLiteral(nameCall.Arguments[0].Expression) ?? variableName;
        }

        // Find the value
        var valueCall = chain.FirstOrDefault(c => c.MethodName.Equals("Value", StringComparison.OrdinalIgnoreCase));
        var value = "undefined";
        if (valueCall?.Arguments.Count > 0)
        {
            value = ConvertExpressionToTs(valueCall.Arguments[0].Expression);
        }

        // Get type if specified
        var typeStr = "";
        if (nameCall?.TypeArguments.Count > 0)
        {
            typeStr = ": " + ConvertTypeToTs(nameCall.TypeArguments[0]);
        }

        return $"{exportKeyword}{keyword} {declaredName}{typeStr} = {value};";
    }

    private string GenerateStoreDeclaration(List<InvocationInfo> chain, string variableName)
    {
        var sb = new StringBuilder();
        var hasExport = chain.Any(c => c.MethodName.Equals("Export", StringComparison.OrdinalIgnoreCase));

        // Extract state
        var stateCall = chain.FirstOrDefault(c => c.MethodName.Equals("WithState", StringComparison.OrdinalIgnoreCase));
        var stateValue = "{}";
        if (stateCall?.Arguments.Count > 0)
        {
            stateValue = ConvertExpressionToTs(stateCall.Arguments[0].Expression);
        }

        sb.Append(hasExport ? "export " : "");
        sb.Append($"const {variableName} = new Store({{\n");
        sb.Append($"  state: {stateValue}\n");
        sb.Append("});");

        return sb.ToString();
    }

    private string GenerateQueryDeclaration(List<InvocationInfo> chain, string variableName)
    {
        var sb = new StringBuilder();
        var hasExport = chain.Any(c => c.MethodName.Equals("Export", StringComparison.OrdinalIgnoreCase));
        var useHelper = chain.Any(c => c.MethodName.Equals("UseHelper", StringComparison.OrdinalIgnoreCase));

        sb.Append(hasExport ? "export " : "");
        sb.Append($"const {variableName} = ");

        if (useHelper)
            sb.Append("queryOptions({\n");
        else
            sb.Append("{\n");

        // Extract query key
        var keyCall = chain.FirstOrDefault(c => c.MethodName.Equals("QueryKey", StringComparison.OrdinalIgnoreCase));
        if (keyCall?.Arguments.Count > 0)
        {
            var keys = keyCall.Arguments.Select(a => ConvertExpressionToTs(a.Expression));
            sb.AppendLine($"  queryKey: [{string.Join(", ", keys)}],");
        }

        // Extract query function
        var fnCall = chain.FirstOrDefault(c => c.MethodName.Equals("QueryFn", StringComparison.OrdinalIgnoreCase));
        if (fnCall?.Arguments.Count > 0)
        {
            sb.AppendLine($"  queryFn: {ConvertExpressionToTs(fnCall.Arguments[0].Expression)},");
        }

        // Extract stale time
        var staleTimeCall = chain.FirstOrDefault(c => c.MethodName.Equals("StaleTime", StringComparison.OrdinalIgnoreCase));
        if (staleTimeCall?.Arguments.Count > 0)
        {
            sb.AppendLine($"  staleTime: {ConvertExpressionToTs(staleTimeCall.Arguments[0].Expression)},");
        }

        if (useHelper)
            sb.Append("});");
        else
            sb.Append("};");

        return sb.ToString();
    }

    private string GenerateRouteDeclaration(List<InvocationInfo> chain, string variableName)
    {
        var sb = new StringBuilder();
        var hasExport = chain.Any(c => c.MethodName.Equals("Export", StringComparison.OrdinalIgnoreCase));

        sb.Append(hasExport ? "export " : "");
        sb.Append($"const {variableName} = createRoute({{\n");

        // Extract path
        var pathCall = chain.FirstOrDefault(c => c.MethodName.Equals("Path", StringComparison.OrdinalIgnoreCase));
        if (pathCall?.Arguments.Count > 0)
        {
            sb.AppendLine($"  path: {ConvertExpressionToTs(pathCall.Arguments[0].Expression)},");
        }

        // Extract component
        var componentCall = chain.FirstOrDefault(c => c.MethodName.Equals("Component", StringComparison.OrdinalIgnoreCase));
        if (componentCall?.Arguments.Count > 0)
        {
            sb.AppendLine($"  component: {ConvertExpressionToTs(componentCall.Arguments[0].Expression)},");
        }

        // Extract loader
        var loaderCall = chain.FirstOrDefault(c => c.MethodName.Equals("Loader", StringComparison.OrdinalIgnoreCase));
        if (loaderCall?.Arguments.Count > 0)
        {
            sb.AppendLine($"  loader: {ConvertExpressionToTs(loaderCall.Arguments[0].Expression)},");
        }

        sb.Append("});");
        return sb.ToString();
    }

    private string GenerateRouterDeclaration(List<InvocationInfo> chain, string variableName)
    {
        var sb = new StringBuilder();
        var hasExport = chain.Any(c => c.MethodName.Equals("Export", StringComparison.OrdinalIgnoreCase));

        sb.Append(hasExport ? "export " : "");
        sb.Append($"const {variableName} = createRouter({{\n");

        // Extract route tree
        var treeCall = chain.FirstOrDefault(c => c.MethodName.Equals("RouteTree", StringComparison.OrdinalIgnoreCase));
        if (treeCall?.Arguments.Count > 0)
        {
            sb.AppendLine($"  routeTree: {ConvertExpressionToTs(treeCall.Arguments[0].Expression)},");
        }

        sb.Append("});");
        return sb.ToString();
    }

    private string GenerateInterfaceDeclaration(List<InvocationInfo> chain, string variableName)
    {
        var sb = new StringBuilder();
        var hasExport = chain.Any(c => c.MethodName.Equals("Export", StringComparison.OrdinalIgnoreCase));

        sb.Append(hasExport ? "export " : "");
        sb.Append($"interface {variableName} {{\n");

        // Extract properties
        var propCalls = chain.Where(c => c.MethodName.Equals("Property", StringComparison.OrdinalIgnoreCase));
        foreach (var propCall in propCalls)
        {
            if (propCall.Arguments.Count >= 1)
            {
                var propName = ExtractStringLiteral(propCall.Arguments[0].Expression) ?? "prop";
                var propType = propCall.TypeArguments.Count > 0
                    ? ConvertTypeToTs(propCall.TypeArguments[0])
                    : "unknown";
                sb.AppendLine($"  {propName}: {propType};");
            }
        }

        sb.Append("}");
        return sb.ToString();
    }

    private string GenerateFunctionDeclaration(List<InvocationInfo> chain, string variableName)
    {
        var sb = new StringBuilder();
        var hasExport = chain.Any(c => c.MethodName.Equals("Export", StringComparison.OrdinalIgnoreCase));
        var isAsync = chain.Any(c => c.MethodName.Equals("Async", StringComparison.OrdinalIgnoreCase));

        sb.Append(hasExport ? "export " : "");
        if (isAsync) sb.Append("async ");
        sb.Append($"function {variableName}(");

        // Extract parameters
        var paramCalls = chain.Where(c => c.MethodName.Equals("Param", StringComparison.OrdinalIgnoreCase));
        var @params = new List<string>();
        foreach (var paramCall in paramCalls)
        {
            if (paramCall.Arguments.Count >= 1)
            {
                var paramName = ExtractStringLiteral(paramCall.Arguments[0].Expression) ?? "param";
                var paramType = paramCall.TypeArguments.Count > 0
                    ? ConvertTypeToTs(paramCall.TypeArguments[0])
                    : "unknown";
                @params.Add($"{paramName}: {paramType}");
            }
        }
        sb.Append(string.Join(", ", @params));
        sb.Append(")");

        // Extract return type
        var returnCall = chain.FirstOrDefault(c => c.MethodName.Equals("Returns", StringComparison.OrdinalIgnoreCase));
        if (returnCall?.TypeArguments.Count > 0)
        {
            sb.Append($": {ConvertTypeToTs(returnCall.TypeArguments[0])}");
        }

        sb.Append(" {\n  // TODO: implement\n}");
        return sb.ToString();
    }

    private string GenerateObjectExpression(List<InvocationInfo> chain, string variableName)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");

        var withCalls = chain.Where(c => c.MethodName.Equals("With", StringComparison.OrdinalIgnoreCase));
        foreach (var withCall in withCalls)
        {
            if (withCall.Arguments.Count >= 2)
            {
                var propName = ExtractStringLiteral(withCall.Arguments[0].Expression) ?? "prop";
                var propValue = ConvertExpressionToTs(withCall.Arguments[1].Expression);
                sb.AppendLine($"  {propName}: {propValue},");
            }
        }

        sb.Append("}");
        return sb.ToString();
    }

    private string GenerateArrowFunction(List<InvocationInfo> chain, string variableName)
    {
        var isAsync = chain.Any(c =>
            c.MethodName.Contains("Async", StringComparison.OrdinalIgnoreCase));

        var sb = new StringBuilder();
        if (isAsync) sb.Append("async ");
        sb.Append("(");

        // Get type arguments for parameter types
        var arrowCall = chain.FirstOrDefault(c =>
            c.MethodName.Contains("Arrow", StringComparison.OrdinalIgnoreCase));

        if (arrowCall != null)
        {
            // Parameter names from string arguments
            var paramNames = arrowCall.Arguments
                .Select(a => ExtractStringLiteral(a.Expression))
                .Where(n => n != null)
                .ToList();

            // Parameter types from type arguments (last one is return type)
            var typeArgs = arrowCall.TypeArguments;
            var paramTypes = typeArgs.Take(typeArgs.Count - 1).ToList();

            var @params = new List<string>();
            for (int i = 0; i < paramNames.Count && i < paramTypes.Count; i++)
            {
                @params.Add($"{paramNames[i]}: {ConvertTypeToTs(paramTypes[i])}");
            }
            sb.Append(string.Join(", ", @params));
        }

        sb.Append(") => ");

        // Get return expression
        var returnsCall = chain.FirstOrDefault(c => c.MethodName.Equals("Returns", StringComparison.OrdinalIgnoreCase));
        if (returnsCall?.Arguments.Count > 0)
        {
            sb.Append(ConvertExpressionToTs(returnsCall.Arguments[0].Expression));
        }
        else
        {
            sb.Append("undefined");
        }

        return sb.ToString();
    }

    private string GenerateGenericExpression(List<InvocationInfo> chain, string variableName)
    {
        // Fallback: just output the method chain as comments
        var sb = new StringBuilder();
        sb.AppendLine($"// Generated from: {variableName}");
        foreach (var call in chain)
        {
            sb.AppendLine($"// - {call.MethodName}({string.Join(", ", call.Arguments.Select(a => a.ToString()))})");
        }
        return sb.ToString();
    }

    private string ConvertExpressionToTs(ExpressionSyntax expression)
    {
        return expression switch
        {
            LiteralExpressionSyntax literal => ConvertLiteralToTs(literal),
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax member => ConvertMemberAccessToTs(member),
            InvocationExpressionSyntax invocation => ConvertInvocationToTs(invocation),
            ObjectCreationExpressionSyntax obj => ConvertObjectCreationToTs(obj),
            LambdaExpressionSyntax lambda => ConvertLambdaToTs(lambda),
            ArrayCreationExpressionSyntax array => ConvertArrayToTs(array),
            ImplicitArrayCreationExpressionSyntax implicitArray => ConvertImplicitArrayToTs(implicitArray),
            CollectionExpressionSyntax collection => ConvertCollectionToTs(collection),
            _ => expression.ToString()
        };
    }

    private static string ConvertLiteralToTs(LiteralExpressionSyntax literal)
    {
        return literal.Kind() switch
        {
            SyntaxKind.StringLiteralExpression => $"'{literal.Token.ValueText}'",
            SyntaxKind.NumericLiteralExpression => literal.Token.Text,
            SyntaxKind.TrueLiteralExpression => "true",
            SyntaxKind.FalseLiteralExpression => "false",
            SyntaxKind.NullLiteralExpression => "null",
            _ => literal.ToString()
        };
    }

    private string ConvertMemberAccessToTs(MemberAccessExpressionSyntax member)
    {
        var obj = ConvertExpressionToTs(member.Expression);
        var prop = member.Name.Identifier.Text;

        // Handle special .NET to TS conversions
        if (obj == "Ts" && prop == "Null") return "null";
        if (obj == "Ts" && prop == "Undefined") return "undefined";
        if (obj == "Ts" && prop == "True") return "true";
        if (obj == "Ts" && prop == "False") return "false";

        return $"{obj}.{prop}";
    }

    private string ConvertInvocationToTs(InvocationExpressionSyntax invocation)
    {
        var method = GetMethodName(invocation.Expression);
        var args = invocation.ArgumentList.Arguments
            .Select(a => ConvertExpressionToTs(a.Expression));

        // Handle Ts.* static methods
        if (invocation.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "Ts" } } member)
        {
            var tsMethod = member.Name.Identifier.Text;
            return tsMethod switch
            {
                "String" => $"'{args.FirstOrDefault() ?? ""}'",
                "Number" or "Int" => args.FirstOrDefault() ?? "0",
                "Bool" => args.FirstOrDefault() ?? "false",
                "Array" => $"[{string.Join(", ", args)}]",
                "Var" => args.FirstOrDefault()?.Trim('\'') ?? "",
                _ => $"{method}({string.Join(", ", args)})"
            };
        }

        return $"{method}({string.Join(", ", args)})";
    }

    private string ConvertObjectCreationToTs(ObjectCreationExpressionSyntax obj)
    {
        var typeName = obj.Type.ToString();
        var args = obj.ArgumentList?.Arguments
            .Select(a => ConvertExpressionToTs(a.Expression)) ?? Enumerable.Empty<string>();

        return $"new {typeName}({string.Join(", ", args)})";
    }

    private string ConvertLambdaToTs(LambdaExpressionSyntax lambda)
    {
        var @params = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
            ParenthesizedLambdaExpressionSyntax paren =>
                string.Join(", ", paren.ParameterList.Parameters.Select(p => p.Identifier.Text)),
            _ => ""
        };

        var body = lambda.ExpressionBody != null
            ? ConvertExpressionToTs(lambda.ExpressionBody)
            : "{ /* body */ }";

        return $"({@params}) => {body}";
    }

    private string ConvertArrayToTs(ArrayCreationExpressionSyntax array)
    {
        if (array.Initializer != null)
        {
            var elements = array.Initializer.Expressions
                .Select(e => ConvertExpressionToTs(e));
            return $"[{string.Join(", ", elements)}]";
        }
        return "[]";
    }

    private string ConvertImplicitArrayToTs(ImplicitArrayCreationExpressionSyntax array)
    {
        var elements = array.Initializer.Expressions
            .Select(e => ConvertExpressionToTs(e));
        return $"[{string.Join(", ", elements)}]";
    }

    private string ConvertCollectionToTs(CollectionExpressionSyntax collection)
    {
        var elements = collection.Elements
            .Select(e => e switch
            {
                ExpressionElementSyntax expr => ConvertExpressionToTs(expr.Expression),
                SpreadElementSyntax spread => $"...{ConvertExpressionToTs(spread.Expression)}",
                _ => e.ToString()
            });
        return $"[{string.Join(", ", elements)}]";
    }

    private static string ConvertTypeToTs(TypeSyntax type)
    {
        var typeStr = type.ToString();

        // Common type mappings
        return typeStr switch
        {
            "string" or "String" => "string",
            "int" or "Int32" or "long" or "Int64" or "short" or "Int16" or
            "double" or "Double" or "float" or "Single" or "decimal" or "Decimal" => "number",
            "bool" or "Boolean" => "boolean",
            "void" or "Void" => "void",
            "object" or "Object" => "unknown",
            "dynamic" => "any",
            _ when typeStr.StartsWith("Task<") => $"Promise<{ConvertTypeToTs(((GenericNameSyntax)type).TypeArgumentList.Arguments[0])}>",
            _ when typeStr.StartsWith("List<") || typeStr.StartsWith("IEnumerable<") ||
                   typeStr.EndsWith("[]") => $"{ConvertTypeToTs(GetElementType(type))}[]",
            _ when typeStr.StartsWith("Dictionary<") || typeStr.StartsWith("IDictionary<") =>
                GetRecordType((GenericNameSyntax)type),
            _ => typeStr
        };
    }

    private static TypeSyntax GetElementType(TypeSyntax type)
    {
        if (type is GenericNameSyntax generic)
            return generic.TypeArgumentList.Arguments[0];
        if (type is ArrayTypeSyntax array)
            return array.ElementType;
        return type;
    }

    private static string GetRecordType(GenericNameSyntax type)
    {
        var args = type.TypeArgumentList.Arguments;
        if (args.Count == 2)
        {
            return $"Record<{ConvertTypeToTs(args[0])}, {ConvertTypeToTs(args[1])}>";
        }
        return "Record<string, unknown>";
    }

    private static string? ExtractStringLiteral(ExpressionSyntax expression)
    {
        if (expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } literal)
        {
            return literal.Token.ValueText;
        }
        return null;
    }
}

internal class InvocationInfo
{
    public string MethodName { get; set; } = "";
    public List<ArgumentSyntax> Arguments { get; set; } = new();
    public List<TypeSyntax> TypeArguments { get; set; } = new();
}

internal enum DeclType
{
    Unknown,
    Const,
    Let,
    Store,
    Query,
    Route,
    Router,
    Interface,
    Function,
    Object,
    Arrow
}

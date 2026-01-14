using System.Text;
using Moj.TanStack.Ast.Core;

namespace Moj.TanStack.Ast.CodeGen;

/// <summary>
/// Emits TypeScript code from AST nodes
/// </summary>
public sealed class TypeScriptEmitter : ITsVisitor<string>
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private const string IndentString = "  ";

    public string Emit(TsNode node)
    {
        _sb.Clear();
        _indentLevel = 0;
        return node.Accept(this);
    }

    private string Indent() => string.Concat(Enumerable.Repeat(IndentString, _indentLevel));

    public string Visit(TsProgram node)
    {
        var parts = new List<string>();
        foreach (var statement in node.Body)
        {
            parts.Add(statement.Accept(this));
        }
        return string.Join("\n\n", parts);
    }

    public string Visit(TsImportDeclaration node)
    {
        var sb = new StringBuilder();
        sb.Append("import ");
        if (node.IsTypeOnly) sb.Append("type ");

        if (node.ImportClause != null)
        {
            var parts = new List<string>();

            if (node.ImportClause.DefaultImport != null)
                parts.Add(node.ImportClause.DefaultImport);

            if (node.ImportClause.NamespaceImport != null)
                parts.Add($"* as {node.ImportClause.NamespaceImport}");

            if (node.ImportClause.NamedImports?.Count > 0)
            {
                var named = node.ImportClause.NamedImports
                    .Select(i => i.Alias != null ? $"{i.Name} as {i.Alias}" : i.Name);
                parts.Add($"{{ {string.Join(", ", named)} }}");
            }

            sb.Append(string.Join(", ", parts));
            sb.Append(" from ");
        }

        sb.Append($"'{node.ModuleSpecifier}';");
        return sb.ToString();
    }

    public string Visit(TsExportDeclaration node)
    {
        var sb = new StringBuilder();
        sb.Append("export ");
        if (node.IsDefault) sb.Append("default ");
        if (node.IsTypeOnly) sb.Append("type ");

        if (node.Declaration != null)
        {
            sb.Append(node.Declaration.Accept(this));
        }
        else if (node.NamedExports?.Count > 0)
        {
            var exports = node.NamedExports
                .Select(e => e.Alias != null ? $"{e.Name} as {e.Alias}" : e.Name);
            sb.Append($"{{ {string.Join(", ", exports)} }}");
            if (node.ModuleSpecifier != null)
                sb.Append($" from '{node.ModuleSpecifier}'");
            sb.Append(';');
        }

        return sb.ToString();
    }

    public string Visit(TsVariableDeclaration node)
    {
        var sb = new StringBuilder();
        if (node.IsExported) sb.Append("export ");

        sb.Append(node.Kind switch
        {
            TsVariableKind.Const => "const",
            TsVariableKind.Let => "let",
            TsVariableKind.Var => "var",
            _ => "const"
        });
        sb.Append(' ');

        var declarators = node.Declarators.Select(d =>
        {
            var decl = new StringBuilder();
            decl.Append(EmitBinding(d.Pattern));
            if (d.Type != null)
                decl.Append($": {d.Type.Accept(this)}");
            if (d.Initializer != null)
                decl.Append($" = {d.Initializer.Accept(this)}");
            return decl.ToString();
        });

        sb.Append(string.Join(", ", declarators));
        sb.Append(';');
        return sb.ToString();
    }

    private string EmitBinding(TsBindingPattern pattern) => pattern switch
    {
        TsIdentifierBinding id => id.Name,
        TsObjectBinding obj => EmitObjectBinding(obj),
        TsArrayBinding arr => EmitArrayBinding(arr),
        _ => throw new NotSupportedException($"Unknown binding pattern: {pattern.GetType().Name}")
    };

    private string EmitObjectBinding(TsObjectBinding obj)
    {
        var elements = obj.Elements.Select(e =>
        {
            var binding = EmitBinding(e.Value);
            if (e.Key != binding)
                return $"{e.Key}: {binding}";
            return binding;
        });
        var rest = obj.Rest != null ? $"...{obj.Rest.Name}" : null;
        var all = rest != null ? elements.Append(rest) : elements;
        return $"{{ {string.Join(", ", all)} }}";
    }

    private string EmitArrayBinding(TsArrayBinding arr)
    {
        var elements = arr.Elements.Select(e => e != null ? EmitBinding(e) : "");
        var rest = arr.Rest != null ? $"...{EmitBinding(arr.Rest)}" : null;
        var all = rest != null ? elements.Append(rest) : elements;
        return $"[{string.Join(", ", all)}]";
    }

    public string Visit(TsFunctionDeclaration node)
    {
        var sb = new StringBuilder();
        if (node.IsExported) sb.Append("export ");
        if (node.IsDefault) sb.Append("default ");
        if (node.IsAsync) sb.Append("async ");
        sb.Append("function ");
        if (node.IsGenerator) sb.Append("* ");
        sb.Append(node.Name);

        if (node.TypeParameters?.Count > 0)
            sb.Append(EmitTypeParameters(node.TypeParameters));

        sb.Append('(');
        sb.Append(string.Join(", ", node.Parameters.Select(p => p.Accept(this))));
        sb.Append(')');

        if (node.ReturnType != null)
            sb.Append($": {node.ReturnType.Accept(this)}");

        sb.Append(' ');
        sb.Append(node.Body.Accept(this));
        return sb.ToString();
    }

    public string Visit(TsArrowFunction node)
    {
        var sb = new StringBuilder();
        if (node.IsAsync) sb.Append("async ");

        if (node.TypeParameters?.Count > 0)
            sb.Append(EmitTypeParameters(node.TypeParameters));

        // Single parameter without type can omit parens
        if (node.Parameters.Count == 1 &&
            node.Parameters[0].Type == null &&
            node.Parameters[0].Pattern is TsIdentifierBinding &&
            !node.Parameters[0].IsOptional &&
            node.Parameters[0].DefaultValue == null)
        {
            sb.Append(EmitBinding(node.Parameters[0].Pattern));
        }
        else
        {
            sb.Append('(');
            sb.Append(string.Join(", ", node.Parameters.Select(p => p.Accept(this))));
            sb.Append(')');
        }

        if (node.ReturnType != null)
            sb.Append($": {node.ReturnType.Accept(this)}");

        sb.Append(" => ");

        if (node.Body is TsBlockStatement block)
            sb.Append(block.Accept(this));
        else if (node.Body is TsObjectLiteral objLiteral)
            // Object literals in arrow function expression bodies need parens: () => ({...})
            sb.Append($"({objLiteral.Accept(this)})");
        else if (node.Body is TsExpression expr)
            sb.Append(expr.Accept(this));
        else
            sb.Append(node.Body.Accept(this));

        return sb.ToString();
    }

    public string Visit(TsCallExpression node)
    {
        var sb = new StringBuilder();
        sb.Append(node.Callee.Accept(this));

        if (node.TypeArguments?.Count > 0)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", node.TypeArguments.Select(t => t.Accept(this))));
            sb.Append('>');
        }

        sb.Append('(');
        sb.Append(string.Join(", ", node.Arguments.Select(a => a.Accept(this))));
        sb.Append(')');
        return sb.ToString();
    }

    public string Visit(TsNewExpression node)
    {
        var sb = new StringBuilder("new ");
        sb.Append(node.Callee.Accept(this));

        if (node.TypeArguments?.Count > 0)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", node.TypeArguments.Select(t => t.Accept(this))));
            sb.Append('>');
        }

        sb.Append('(');
        sb.Append(string.Join(", ", node.Arguments.Select(a => a.Accept(this))));
        sb.Append(')');
        return sb.ToString();
    }

    public string Visit(TsMemberAccess node)
    {
        return $"{node.Object.Accept(this)}.{node.Property}";
    }

    public string Visit(TsIndexAccess node)
    {
        return $"{node.Object.Accept(this)}[{node.Index.Accept(this)}]";
    }

    public string Visit(TsObjectLiteral node)
    {
        if (node.Properties.Count == 0)
            return "{}";

        var props = node.Properties.Select(p => p.Accept(this));

        // Multi-line for more than 3 properties or complex content
        if (node.Properties.Count > 3)
        {
            _indentLevel++;
            var indented = props.Select(p => $"{Indent()}{p}");
            _indentLevel--;
            return $"{{\n{string.Join(",\n", indented)}\n{Indent()}}}";
        }

        return $"{{ {string.Join(", ", props)} }}";
    }

    public string Visit(TsArrayLiteral node)
    {
        var elements = node.Elements.Select(e => e?.Accept(this) ?? "");
        return $"[{string.Join(", ", elements)}]";
    }

    public string Visit(TsPropertyAssignment node)
    {
        var key = node.Key switch
        {
            TsIdentifier id => id.Name,
            TsLiteral lit => lit.Accept(this),
            _ => node.IsComputed ? $"[{node.Key.Accept(this)}]" : node.Key.Accept(this)
        };

        if (node.IsShorthand)
            return key;

        return $"{key}: {node.Value.Accept(this)}";
    }

    public string Visit(TsSpreadElement node)
    {
        return $"...{node.Expression.Accept(this)}";
    }

    public string Visit(TsIdentifier node) => node.Name;

    public string Visit(TsLiteral node)
    {
        return node.Kind switch
        {
            TsLiteralKind.String => $"'{EscapeString(node.Value?.ToString() ?? "")}'",
            TsLiteralKind.Number => node.Value?.ToString() ?? "0",
            TsLiteralKind.Boolean => node.Value is true ? "true" : "false",
            TsLiteralKind.Null => "null",
            TsLiteralKind.Undefined => "undefined",
            TsLiteralKind.BigInt => $"{node.Value}n",
            TsLiteralKind.Regex => node.Value?.ToString() ?? "//",
            _ => node.Value?.ToString() ?? ""
        };
    }

    private static string EscapeString(string s)
    {
        return s
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    public string Visit(TsTemplateLiteral node)
    {
        var sb = new StringBuilder("`");
        for (int i = 0; i < node.Quasis.Count; i++)
        {
            sb.Append(node.Quasis[i]);
            if (i < node.Expressions.Count)
                sb.Append($"${{{node.Expressions[i].Accept(this)}}}");
        }
        sb.Append('`');
        return sb.ToString();
    }

    public string Visit(TsBinaryExpression node)
    {
        var op = GetBinaryOperatorString(node.Operator);
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);

        // Add parens for nested binary expressions
        if (node.Left is TsBinaryExpression)
            left = $"({left})";
        if (node.Right is TsBinaryExpression)
            right = $"({right})";

        return $"{left} {op} {right}";
    }

    private static string GetBinaryOperatorString(TsBinaryOperator op) => op switch
    {
        TsBinaryOperator.Add => "+",
        TsBinaryOperator.Subtract => "-",
        TsBinaryOperator.Multiply => "*",
        TsBinaryOperator.Divide => "/",
        TsBinaryOperator.Modulo => "%",
        TsBinaryOperator.Power => "**",
        TsBinaryOperator.Equal => "==",
        TsBinaryOperator.StrictEqual => "===",
        TsBinaryOperator.NotEqual => "!=",
        TsBinaryOperator.StrictNotEqual => "!==",
        TsBinaryOperator.LessThan => "<",
        TsBinaryOperator.LessThanOrEqual => "<=",
        TsBinaryOperator.GreaterThan => ">",
        TsBinaryOperator.GreaterThanOrEqual => ">=",
        TsBinaryOperator.And => "&&",
        TsBinaryOperator.Or => "||",
        TsBinaryOperator.NullishCoalesce => "??",
        TsBinaryOperator.BitwiseAnd => "&",
        TsBinaryOperator.BitwiseOr => "|",
        TsBinaryOperator.BitwiseXor => "^",
        TsBinaryOperator.LeftShift => "<<",
        TsBinaryOperator.RightShift => ">>",
        TsBinaryOperator.UnsignedRightShift => ">>>",
        TsBinaryOperator.In => "in",
        TsBinaryOperator.InstanceOf => "instanceof",
        TsBinaryOperator.Assign => "=",
        TsBinaryOperator.AddAssign => "+=",
        TsBinaryOperator.SubtractAssign => "-=",
        TsBinaryOperator.MultiplyAssign => "*=",
        TsBinaryOperator.DivideAssign => "/=",
        TsBinaryOperator.ModuloAssign => "%=",
        TsBinaryOperator.AndAssign => "&&=",
        TsBinaryOperator.OrAssign => "||=",
        TsBinaryOperator.NullishAssign => "??=",
        _ => throw new NotSupportedException($"Unknown operator: {op}")
    };

    public string Visit(TsUnaryExpression node)
    {
        var op = GetUnaryOperatorString(node.Operator);
        var operand = node.Operand.Accept(this);

        return node.IsPrefix
            ? node.Operator is TsUnaryOperator.TypeOf or TsUnaryOperator.Void or TsUnaryOperator.Delete or TsUnaryOperator.Await
                ? $"{op} {operand}"
                : $"{op}{operand}"
            : $"{operand}{op}";
    }

    private static string GetUnaryOperatorString(TsUnaryOperator op) => op switch
    {
        TsUnaryOperator.Negate => "-",
        TsUnaryOperator.Plus => "+",
        TsUnaryOperator.Not => "!",
        TsUnaryOperator.BitwiseNot => "~",
        TsUnaryOperator.TypeOf => "typeof",
        TsUnaryOperator.Void => "void",
        TsUnaryOperator.Delete => "delete",
        TsUnaryOperator.Increment => "++",
        TsUnaryOperator.Decrement => "--",
        TsUnaryOperator.Await => "await",
        TsUnaryOperator.Spread => "...",
        _ => throw new NotSupportedException($"Unknown operator: {op}")
    };

    public string Visit(TsConditionalExpression node)
    {
        return $"{node.Condition.Accept(this)} ? {node.WhenTrue.Accept(this)} : {node.WhenFalse.Accept(this)}";
    }

    public string Visit(TsAwaitExpression node)
    {
        return $"await {node.Expression.Accept(this)}";
    }

    public string Visit(TsAsExpression node)
    {
        return $"{node.Expression.Accept(this)} as {node.Type.Accept(this)}";
    }

    public string Visit(TsTypeAssertion node)
    {
        return $"<{node.Type.Accept(this)}>{node.Expression.Accept(this)}";
    }

    public string Visit(TsReturnStatement node)
    {
        return node.Expression != null
            ? $"return {node.Expression.Accept(this)};"
            : "return;";
    }

    public string Visit(TsIfStatement node)
    {
        var sb = new StringBuilder();
        sb.Append($"if ({node.Condition.Accept(this)}) ");
        sb.Append(node.ThenBranch.Accept(this));

        if (node.ElseBranch != null)
        {
            sb.Append(" else ");
            sb.Append(node.ElseBranch.Accept(this));
        }

        return sb.ToString();
    }

    public string Visit(TsBlockStatement node)
    {
        if (node.Statements.Count == 0)
            return "{}";

        _indentLevel++;
        var statements = node.Statements.Select(s =>
        {
            var stmt = s.Accept(this);
            // Add semicolon if not already present and not a block/function
            if (!stmt.EndsWith(";") && !stmt.EndsWith("}"))
                stmt += ";";
            return $"{Indent()}{stmt}";
        });
        _indentLevel--;

        return $"{{\n{string.Join("\n", statements)}\n{Indent()}}}";
    }

    public string Visit(TsTypeReference node)
    {
        return node.Qualifier != null ? $"{node.Qualifier}.{node.Name}" : node.Name;
    }

    public string Visit(TsGenericType node)
    {
        var args = string.Join(", ", node.TypeArguments.Select(t => t.Accept(this)));
        return $"{node.BaseType.Accept(this)}<{args}>";
    }

    public string Visit(TsUnionType node)
    {
        return string.Join(" | ", node.Types.Select(t => t.Accept(this)));
    }

    public string Visit(TsIntersectionType node)
    {
        return string.Join(" & ", node.Types.Select(t => t.Accept(this)));
    }

    public string Visit(TsObjectType node)
    {
        if (node.Members.Count == 0)
            return "{}";

        var members = node.Members.Select(EmitTypeMember);

        if (node.Members.Count <= 3)
            return $"{{ {string.Join("; ", members)} }}";

        _indentLevel++;
        var indented = members.Select(m => $"{Indent()}{m};");
        _indentLevel--;
        return $"{{\n{string.Join("\n", indented)}\n{Indent()}}}";
    }

    private string EmitTypeMember(TsTypeMember member) => member switch
    {
        TsPropertySignature prop =>
            $"{(prop.IsReadonly ? "readonly " : "")}{prop.Name}{(prop.IsOptional ? "?" : "")}: {prop.Type.Accept(this)}",
        TsMethodSignature method =>
            $"{method.Name}{(method.IsOptional ? "?" : "")}{EmitTypeParameters(method.TypeParameters)}({string.Join(", ", method.Parameters.Select(p => p.Accept(this)))}){(method.ReturnType != null ? $": {method.ReturnType.Accept(this)}" : "")}",
        TsIndexSignature idx =>
            $"{(idx.IsReadonly ? "readonly " : "")}[{idx.ParameterName}: {idx.KeyType.Accept(this)}]: {idx.ValueType.Accept(this)}",
        TsCallSignature call =>
            $"{EmitTypeParameters(call.TypeParameters)}({string.Join(", ", call.Parameters.Select(p => p.Accept(this)))}){(call.ReturnType != null ? $": {call.ReturnType.Accept(this)}" : "")}",
        TsConstructSignature ctor =>
            $"new {EmitTypeParameters(ctor.TypeParameters)}({string.Join(", ", ctor.Parameters.Select(p => p.Accept(this)))}){(ctor.ReturnType != null ? $": {ctor.ReturnType.Accept(this)}" : "")}",
        _ => throw new NotSupportedException($"Unknown type member: {member.GetType().Name}")
    };

    public string Visit(TsArrayType node)
    {
        var element = node.ElementType.Accept(this);
        // Wrap union types in parens
        if (node.ElementType is TsUnionType or TsIntersectionType or TsFunctionType)
            element = $"({element})";
        return $"{element}[]";
    }

    public string Visit(TsFunctionType node)
    {
        var sb = new StringBuilder();
        sb.Append(EmitTypeParameters(node.TypeParameters));
        sb.Append('(');
        sb.Append(string.Join(", ", node.Parameters.Select(p => p.Accept(this))));
        sb.Append(") => ");
        sb.Append(node.ReturnType.Accept(this));
        return sb.ToString();
    }

    public string Visit(TsParameter node)
    {
        var sb = new StringBuilder();
        if (node.IsRest) sb.Append("...");
        sb.Append(EmitBinding(node.Pattern));
        if (node.IsOptional) sb.Append('?');
        if (node.Type != null)
            sb.Append($": {node.Type.Accept(this)}");
        if (node.DefaultValue != null)
            sb.Append($" = {node.DefaultValue.Accept(this)}");
        return sb.ToString();
    }

    public string Visit(TsInterfaceDeclaration node)
    {
        var sb = new StringBuilder();
        if (node.IsExported) sb.Append("export ");
        sb.Append("interface ");
        sb.Append(node.Name);
        sb.Append(EmitTypeParameters(node.TypeParameters));

        if (node.Extends?.Count > 0)
        {
            sb.Append(" extends ");
            sb.Append(string.Join(", ", node.Extends.Select(t => t.Accept(this))));
        }

        sb.Append(" {\n");
        _indentLevel++;
        foreach (var member in node.Members)
        {
            sb.Append(Indent());
            sb.Append(EmitTypeMember(member));
            sb.Append(";\n");
        }
        _indentLevel--;
        sb.Append(Indent());
        sb.Append('}');
        return sb.ToString();
    }

    public string Visit(TsTypeAliasDeclaration node)
    {
        var sb = new StringBuilder();
        if (node.IsExported) sb.Append("export ");
        sb.Append("type ");
        sb.Append(node.Name);
        sb.Append(EmitTypeParameters(node.TypeParameters));
        sb.Append(" = ");
        sb.Append(node.Type.Accept(this));
        sb.Append(';');
        return sb.ToString();
    }

    public string Visit(TsLiteralType node)
    {
        // TsLiteralType wraps a TsLiteral, emit the literal's representation
        return node.Value.Accept(this);
    }

    public string Visit(TsComment node)
    {
        if (node.IsMultiLine)
            return $"/* {node.Text} */";
        return $"// {node.Text}";
    }

    private string EmitTypeParameters(IReadOnlyList<TsTypeParameter>? typeParams)
    {
        if (typeParams == null || typeParams.Count == 0)
            return "";

        var parts = typeParams.Select(tp =>
        {
            var sb = new StringBuilder(tp.Name);
            if (tp.Constraint != null)
                sb.Append($" extends {tp.Constraint.Accept(this)}");
            if (tp.Default != null)
                sb.Append($" = {tp.Default.Accept(this)}");
            return sb.ToString();
        });

        return $"<{string.Join(", ", parts)}>";
    }
}

using System.Text;
using Impulse.SourceGen.Ast;

namespace Impulse.SourceGen.Emitters;

/// <summary>
/// Emits TypeScript code from AST nodes.
/// Produces clean, formatted TypeScript output.
/// </summary>
public class TsEmitter
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private const string IndentString = "  ";

    public string Emit(TsFile file)
    {
        _sb.Clear();
        _indent = 0;

        foreach (var statement in file.Statements)
        {
            EmitNode(statement);
            _sb.AppendLine();
        }

        return _sb.ToString().TrimEnd();
    }

    public string EmitNode(TsNode node)
    {
        switch (node)
        {
            case TsImport import:
                EmitImport(import);
                break;
            case TsInterface iface:
                EmitInterface(iface);
                break;
            case TsConst constant:
                EmitConst(constant);
                break;
            case TsFunction func:
                EmitFunction(func);
                break;
            case TsTypeAlias alias:
                EmitTypeAlias(alias);
                break;
            case TsEnum enumNode:
                EmitEnum(enumNode);
                break;
        }
        return _sb.ToString();
    }

    private void EmitImport(TsImport import)
    {
        var typePrefix = import.IsType ? "type " : "";
        var names = string.Join(", ", import.Names);
        AppendLine($"import {typePrefix}{{ {names} }} from '{import.From}';");
    }

    private void EmitInterface(TsInterface iface)
    {
        AppendLine($"export interface {iface.Name} {{");
        _indent++;
        foreach (var prop in iface.Properties)
        {
            EmitProperty(prop);
        }
        _indent--;
        AppendLine("}");
    }

    private void EmitProperty(TsProperty prop)
    {
        var optional = prop.Optional ? "?" : "";
        var type = EmitType(prop.Type);
        AppendLine($"{prop.Name}{optional}: {type};");
    }

    private void EmitConst(TsConst constant)
    {
        var asConst = constant.AsConst ? " as const" : "";
        Append($"export const {constant.Name} = ");
        EmitExpression(constant.Value);
        _sb.Append(asConst);
        _sb.AppendLine(";");
    }

    private void EmitFunction(TsFunction func)
    {
        var parameters = string.Join(", ", func.Parameters.Select(p =>
            $"{p.Name}: {EmitType(p.Type)}"));
        var returnType = EmitType(func.ReturnType);

        AppendLine($"export function {func.Name}({parameters}): {returnType} {{");
        _indent++;
        EmitExpression(func.Body);
        _indent--;
        AppendLine("}");
    }

    private void EmitTypeAlias(TsTypeAlias alias)
    {
        var type = EmitType(alias.Type);
        AppendLine($"export type {alias.Name} = {type};");
    }

    private void EmitEnum(TsEnum enumNode)
    {
        AppendLine($"export enum {enumNode.Name} {{");
        _indent++;
        foreach (var member in enumNode.Members)
        {
            if (member.Value != null)
            {
                AppendLine($"{member.Name} = {member.Value},");
            }
            else
            {
                AppendLine($"{member.Name},");
            }
        }
        _indent--;
        AppendLine("}");
    }

    public string EmitType(TsType type) => type switch
    {
        TsString => "string",
        TsNumber => "number",
        TsBoolean => "boolean",
        TsNull => "null",
        TsUndefined => "undefined",
        TsUnknown => "unknown",
        TsVoid => "void",
        TsArray arr => $"readonly {EmitType(arr.Element)}[]",
        TsRecord rec => $"Record<{EmitType(rec.Key)}, {EmitType(rec.Value)}>",
        TsRef r => r.Name,
        TsGeneric gen => $"{gen.Name}<{string.Join(", ", gen.TypeArgs.Select(EmitType))}>",
        TsUnion union => string.Join(" | ", union.Types.Select(EmitType)),
        TsIntersection inter => string.Join(" & ", inter.Types.Select(EmitType)),
        TsLiteral lit => lit.Value,
        TsObjectType obj => EmitObjectType(obj),
        TsFunctionType func => EmitFunctionType(func),
        TsTuple tuple => $"[{string.Join(", ", tuple.Elements.Select(EmitType))}]",
        _ => "unknown"
    };

    private string EmitObjectType(TsObjectType obj)
    {
        if (obj.Properties.Count == 0) return "{}";

        var props = obj.Properties.Select(p =>
        {
            var optional = p.Optional ? "?" : "";
            return $"{p.Name}{optional}: {EmitType(p.Type)}";
        });
        return $"{{ {string.Join("; ", props)} }}";
    }

    private string EmitFunctionType(TsFunctionType func)
    {
        var parameters = string.Join(", ", func.Parameters.Select(p =>
            $"{p.Name}: {EmitType(p.Type)}"));
        return $"({parameters}) => {EmitType(func.ReturnType)}";
    }

    public void EmitExpression(TsExpression expr)
    {
        switch (expr)
        {
            case TsStringLiteral str:
                _sb.Append($"'{EscapeString(str.Value)}'");
                break;

            case TsNumberLiteral num:
                _sb.Append(num.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                break;

            case TsBoolLiteral b:
                _sb.Append(b.Value ? "true" : "false");
                break;

            case TsIdentifier id:
                _sb.Append(id.Name);
                break;

            case TsObjectLiteral obj:
                EmitObjectLiteral(obj);
                break;

            case TsArrayLiteral arr:
                EmitArrayLiteral(arr);
                break;

            case TsCall call:
                EmitCall(call);
                break;

            case TsMemberAccess member:
                EmitExpression(member.Object);
                _sb.Append('.');
                _sb.Append(member.Property);
                break;

            case TsArrowFunction arrow:
                EmitArrowFunction(arrow);
                break;

            case TsTemplateLiteral template:
                EmitTemplateLiteral(template);
                break;

            case TsAwait await:
                _sb.Append("await ");
                EmitExpression(await.Expression);
                break;

            case TsReturn ret:
                _sb.Append("return ");
                if (ret.Expression != null)
                {
                    EmitExpression(ret.Expression);
                }
                break;

            case TsAssertion assertion:
                EmitExpression(assertion.Expression);
                _sb.Append(" as ");
                _sb.Append(EmitType(assertion.Type));
                break;
        }
    }

    private void EmitObjectLiteral(TsObjectLiteral obj)
    {
        if (obj.Properties.Count == 0)
        {
            _sb.Append("{}");
            return;
        }

        _sb.AppendLine("{");
        _indent++;
        for (var i = 0; i < obj.Properties.Count; i++)
        {
            var prop = obj.Properties[i];
            Append("");
            if (prop.Shorthand)
            {
                _sb.Append(prop.Key);
            }
            else
            {
                _sb.Append(prop.Key);
                _sb.Append(": ");
                EmitExpression(prop.Value);
            }
            if (i < obj.Properties.Count - 1)
            {
                _sb.Append(',');
            }
            _sb.AppendLine();
        }
        _indent--;
        Append("}");
    }

    private void EmitArrayLiteral(TsArrayLiteral arr)
    {
        _sb.Append('[');
        for (var i = 0; i < arr.Elements.Count; i++)
        {
            if (i > 0) _sb.Append(", ");
            EmitExpression(arr.Elements[i]);
        }
        _sb.Append(']');
    }

    private void EmitCall(TsCall call)
    {
        EmitExpression(call.Callee);
        _sb.Append('(');
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            if (i > 0) _sb.Append(", ");
            EmitExpression(call.Arguments[i]);
        }
        _sb.Append(')');
    }

    private void EmitArrowFunction(TsArrowFunction arrow)
    {
        if (arrow.IsAsync) _sb.Append("async ");

        if (arrow.Parameters.Count == 1 && arrow.Parameters[0].Type is TsUnknown)
        {
            _sb.Append(arrow.Parameters[0].Name);
        }
        else
        {
            _sb.Append('(');
            for (var i = 0; i < arrow.Parameters.Count; i++)
            {
                if (i > 0) _sb.Append(", ");
                var p = arrow.Parameters[i];
                _sb.Append(p.Name);
                if (p.Type is not TsUnknown)
                {
                    _sb.Append(": ");
                    _sb.Append(EmitType(p.Type));
                }
            }
            _sb.Append(')');
        }

        _sb.Append(" => ");
        EmitExpression(arrow.Body);
    }

    private void EmitTemplateLiteral(TsTemplateLiteral template)
    {
        _sb.Append('`');
        foreach (var span in template.Spans)
        {
            if (span.Text != null)
            {
                _sb.Append(span.Text);
            }
            if (span.Expression != null)
            {
                _sb.Append("${");
                EmitExpression(span.Expression);
                _sb.Append('}');
            }
        }
        _sb.Append('`');
    }

    private void Append(string text)
    {
        _sb.Append(new string(' ', _indent * IndentString.Length));
        _sb.Append(text);
    }

    private void AppendLine(string text)
    {
        _sb.Append(new string(' ', _indent * IndentString.Length));
        _sb.AppendLine(text);
    }

    private static string EscapeString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

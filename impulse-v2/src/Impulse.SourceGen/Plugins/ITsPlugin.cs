using Impulse.SourceGen.Ast;

namespace Impulse.SourceGen.Plugins;

/// <summary>
/// Plugin interface for transforming TypeScript AST.
/// Plugins are applied in order during code generation.
/// </summary>
public interface ITsPlugin
{
    /// <summary>
    /// Transform the TypeScript AST.
    /// Return a modified copy (AST nodes are immutable records).
    /// </summary>
    TsFile Transform(TsFile ast);

    /// <summary>
    /// Plugin priority (lower = runs first).
    /// </summary>
    int Priority => 100;
}

/// <summary>
/// Plugin interface for transforming Zod schema AST.
/// </summary>
public interface IZodPlugin
{
    /// <summary>
    /// Transform the Zod AST.
    /// </summary>
    ZodFile Transform(ZodFile ast);

    /// <summary>
    /// Plugin priority (lower = runs first).
    /// </summary>
    int Priority => 100;
}

/// <summary>
/// Plugin interface for transforming route AST.
/// </summary>
public interface IRoutePlugin
{
    /// <summary>
    /// Transform the route file AST.
    /// </summary>
    RouteFile Transform(RouteFile ast);

    /// <summary>
    /// Plugin priority (lower = runs first).
    /// </summary>
    int Priority => 100;
}

/// <summary>
/// Base class for plugins that need to traverse and transform AST nodes.
/// Override specific Visit methods to transform nodes.
/// </summary>
public abstract class TsPluginBase : ITsPlugin
{
    public virtual int Priority => 100;

    public virtual TsFile Transform(TsFile ast)
    {
        var transformed = ast.Statements.Select(Visit).ToList();
        return ast with { Statements = transformed };
    }

    protected virtual TsNode Visit(TsNode node) => node switch
    {
        TsInterface iface => VisitInterface(iface),
        TsImport import => VisitImport(import),
        TsConst constant => VisitConst(constant),
        TsFunction func => VisitFunction(func),
        TsTypeAlias alias => VisitTypeAlias(alias),
        TsEnum enumNode => VisitEnum(enumNode),
        _ => node
    };

    protected virtual TsInterface VisitInterface(TsInterface node)
    {
        var props = node.Properties.Select(VisitProperty).ToList();
        return node with { Properties = props };
    }

    protected virtual TsProperty VisitProperty(TsProperty prop)
    {
        var type = VisitType(prop.Type);
        return prop with { Type = type };
    }

    protected virtual TsType VisitType(TsType type) => type switch
    {
        TsArray arr => arr with { Element = VisitType(arr.Element) },
        TsRecord rec => rec with { Key = VisitType(rec.Key), Value = VisitType(rec.Value) },
        TsUnion union => union with { Types = union.Types.Select(VisitType).ToList() },
        TsIntersection inter => inter with { Types = inter.Types.Select(VisitType).ToList() },
        TsObjectType obj => obj with { Properties = obj.Properties.Select(VisitProperty).ToList() },
        TsGeneric gen => gen with { TypeArgs = gen.TypeArgs.Select(VisitType).ToList() },
        TsTuple tuple => tuple with { Elements = tuple.Elements.Select(VisitType).ToList() },
        TsFunctionType func => func with
        {
            Parameters = func.Parameters.Select(p => p with { Type = VisitType(p.Type) }).ToList(),
            ReturnType = VisitType(func.ReturnType)
        },
        _ => type
    };

    protected virtual TsImport VisitImport(TsImport node) => node;
    protected virtual TsConst VisitConst(TsConst node) => node;
    protected virtual TsFunction VisitFunction(TsFunction node) => node;
    protected virtual TsTypeAlias VisitTypeAlias(TsTypeAlias node) => node;
    protected virtual TsEnum VisitEnum(TsEnum node) => node;
}

/// <summary>
/// Plugin that adds readonly modifier to all array types.
/// </summary>
public class ReadonlyArrayPlugin : TsPluginBase
{
    public override int Priority => 50;

    // Arrays are already handled in emitter with 'readonly' prefix
    // This plugin is an example of AST transformation
}

/// <summary>
/// Plugin that adds JSDoc comments to interfaces.
/// </summary>
public class JsDocPlugin : ITsPlugin
{
    private readonly Dictionary<string, string> _descriptions;

    public JsDocPlugin(Dictionary<string, string>? descriptions = null)
    {
        _descriptions = descriptions ?? new Dictionary<string, string>();
    }

    public int Priority => 200;

    public TsFile Transform(TsFile ast)
    {
        // JSDoc comments would be added during emission
        // This plugin could add metadata to nodes for the emitter
        return ast;
    }
}

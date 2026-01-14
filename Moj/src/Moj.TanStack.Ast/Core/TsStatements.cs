namespace Moj.TanStack.Ast.Core;

/// <summary>
/// Base class for statements
/// </summary>
public abstract record TsStatement : TsNode;

/// <summary>
/// Program/module root node
/// </summary>
public sealed record TsProgram(IReadOnlyList<TsNode> Body) : TsNode
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Block statement { ... }
/// </summary>
public sealed record TsBlockStatement(IReadOnlyList<TsNode> Statements) : TsStatement
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Return statement
/// </summary>
public sealed record TsReturnStatement(TsExpression? Expression = null) : TsStatement
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// If statement
/// </summary>
public sealed record TsIfStatement(
    TsExpression Condition,
    TsNode ThenBranch,
    TsNode? ElseBranch = null
) : TsStatement
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

/// <summary>
/// Expression statement (expression used as statement)
/// </summary>
public sealed record TsExpressionStatement(TsExpression Expression) : TsStatement
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Throw statement
/// </summary>
public sealed record TsThrowStatement(TsExpression Expression) : TsStatement
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Try-catch-finally statement
/// </summary>
public sealed record TsTryCatchStatement(
    TsBlockStatement TryBlock,
    TsIdentifier? CatchParameter,
    TsBlockStatement? CatchBlock,
    TsBlockStatement? FinallyBlock
) : TsStatement
{
    public override void Accept(ITsVisitor visitor) => throw new NotImplementedException();
    public override T Accept<T>(ITsVisitor<T> visitor) => throw new NotImplementedException();
}

/// <summary>
/// Comment node
/// </summary>
public sealed record TsComment(string Text, bool IsMultiLine = false) : TsNode
{
    public override void Accept(ITsVisitor visitor) => visitor.Visit(this);
    public override T Accept<T>(ITsVisitor<T> visitor) => visitor.Visit(this);
}

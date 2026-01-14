namespace Moj.TanStack.Ast.Core;

/// <summary>
/// Base class for all TypeScript AST nodes
/// </summary>
public abstract record TsNode
{
    public abstract void Accept(ITsVisitor visitor);
    public abstract T Accept<T>(ITsVisitor<T> visitor);
}

/// <summary>
/// Visitor interface for traversing the TypeScript AST
/// </summary>
public interface ITsVisitor
{
    void Visit(TsProgram node);
    void Visit(TsImportDeclaration node);
    void Visit(TsExportDeclaration node);
    void Visit(TsVariableDeclaration node);
    void Visit(TsFunctionDeclaration node);
    void Visit(TsArrowFunction node);
    void Visit(TsCallExpression node);
    void Visit(TsNewExpression node);
    void Visit(TsMemberAccess node);
    void Visit(TsIndexAccess node);
    void Visit(TsObjectLiteral node);
    void Visit(TsArrayLiteral node);
    void Visit(TsPropertyAssignment node);
    void Visit(TsSpreadElement node);
    void Visit(TsIdentifier node);
    void Visit(TsLiteral node);
    void Visit(TsTemplateLiteral node);
    void Visit(TsBinaryExpression node);
    void Visit(TsUnaryExpression node);
    void Visit(TsConditionalExpression node);
    void Visit(TsAwaitExpression node);
    void Visit(TsAsExpression node);
    void Visit(TsTypeAssertion node);
    void Visit(TsReturnStatement node);
    void Visit(TsIfStatement node);
    void Visit(TsBlockStatement node);
    void Visit(TsTypeReference node);
    void Visit(TsGenericType node);
    void Visit(TsUnionType node);
    void Visit(TsIntersectionType node);
    void Visit(TsObjectType node);
    void Visit(TsArrayType node);
    void Visit(TsFunctionType node);
    void Visit(TsParameter node);
    void Visit(TsInterfaceDeclaration node);
    void Visit(TsTypeAliasDeclaration node);
    void Visit(TsLiteralType node);
    void Visit(TsComment node);
}

/// <summary>
/// Generic visitor interface returning a value
/// </summary>
public interface ITsVisitor<T>
{
    T Visit(TsProgram node);
    T Visit(TsImportDeclaration node);
    T Visit(TsExportDeclaration node);
    T Visit(TsVariableDeclaration node);
    T Visit(TsFunctionDeclaration node);
    T Visit(TsArrowFunction node);
    T Visit(TsCallExpression node);
    T Visit(TsNewExpression node);
    T Visit(TsMemberAccess node);
    T Visit(TsIndexAccess node);
    T Visit(TsObjectLiteral node);
    T Visit(TsArrayLiteral node);
    T Visit(TsPropertyAssignment node);
    T Visit(TsSpreadElement node);
    T Visit(TsIdentifier node);
    T Visit(TsLiteral node);
    T Visit(TsTemplateLiteral node);
    T Visit(TsBinaryExpression node);
    T Visit(TsUnaryExpression node);
    T Visit(TsConditionalExpression node);
    T Visit(TsAwaitExpression node);
    T Visit(TsAsExpression node);
    T Visit(TsTypeAssertion node);
    T Visit(TsReturnStatement node);
    T Visit(TsIfStatement node);
    T Visit(TsBlockStatement node);
    T Visit(TsTypeReference node);
    T Visit(TsGenericType node);
    T Visit(TsUnionType node);
    T Visit(TsIntersectionType node);
    T Visit(TsObjectType node);
    T Visit(TsArrayType node);
    T Visit(TsFunctionType node);
    T Visit(TsParameter node);
    T Visit(TsInterfaceDeclaration node);
    T Visit(TsTypeAliasDeclaration node);
    T Visit(TsLiteralType node);
    T Visit(TsComment node);
}

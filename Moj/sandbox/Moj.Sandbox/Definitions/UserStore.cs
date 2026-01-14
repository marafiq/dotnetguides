using Moj.TanStack.Dsl.Core;
using Moj.TanStack.Dsl.TanStack.Store;
using Moj.TanStack.SourceGenerator.Attributes;

namespace Moj.Sandbox.Definitions;

/// <summary>
/// User state interface for type safety
/// </summary>
public interface IUserState
{
    string Name { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    string[] Roles { get; }
    UserPreferences Preferences { get; }
}

public interface UserPreferences
{
    string Theme { get; }
    string Language { get; }
    bool Notifications { get; }
}

/// <summary>
/// DSL definition for a TanStack Store managing user state
/// </summary>
[TsStore(Name = "userStore")]
public static class UserStore
{
    /// <summary>
    /// The imports needed for this store
    /// </summary>
    public static TsImportDeclaration Imports => TanStackStore.Import();

    /// <summary>
    /// React store adapter import
    /// </summary>
    public static TsImportDeclaration ReactImports => TanStackStore.ImportReact();

    /// <summary>
    /// Type definition for user state
    /// </summary>
    public static TsInterfaceDeclaration UserStateInterface =>
        Ts.Interface("UserState")
            .Property<string>("name")
            .Property<string>("email")
            .Property<bool>("isAuthenticated")
            .Property("roles", new Moj.TanStack.Ast.Core.TsArrayType(Moj.TanStack.Ast.Core.TsPrimitiveTypes.String))
            .Property("preferences", Ts.TypeRef("UserPreferences"))
            .Export()
            .Build();

    /// <summary>
    /// Type definition for user preferences
    /// </summary>
    public static TsInterfaceDeclaration PreferencesInterface =>
        Ts.Interface("UserPreferences")
            .Property<string>("theme")
            .Property<string>("language")
            .Property<bool>("notifications")
            .Export()
            .Build();

    /// <summary>
    /// Initial state for the user store
    /// </summary>
    public static TsExpr<object> InitialState =>
        Ts.Object<object>()
            .With("name", Ts.String(""))
            .With("email", Ts.String(""))
            .With("isAuthenticated", Ts.False)
            .With("roles", Ts.EmptyArray<string>())
            .With("preferences", Ts.Object<object>()
                .With("theme", Ts.String("light"))
                .With("language", Ts.String("en"))
                .With("notifications", Ts.True)
                .Build())
            .Build();

    /// <summary>
    /// The store definition using TanStack Store DSL
    /// </summary>
    public static TsVariableDeclaration Store =>
        TanStackStore.CreateStore<IUserState>()
            .WithState(InitialState)
            .As("userStore")
            .Export()
            .Build();

    /// <summary>
    /// Action to set user data
    /// </summary>
    public static TsVariableDeclaration SetUserAction =>
        Ts.Const<object>("setUser")
            .Value(Ts.Arrow<object, object>("user")
                .Returns(u =>
                    Ts.Var("userStore").Dot("setState").Invoke(
                        Ts.Arrow<object, object>("state").Returns(_ =>
                            Ts.Object<object>()
                                .Spread(Ts.Var("state"))
                                .Spread(u)
                                .With("isAuthenticated", Ts.True)
                                .Build()
                        )
                    ).Typed<object>()
                ))
            .Export()
            .Build();

    /// <summary>
    /// Action to log out
    /// </summary>
    public static TsVariableDeclaration LogoutAction =>
        Ts.Const<object>("logout")
            .Value(Ts.Arrow<object>().Returns(InitialState))
            .Export()
            .Build();

    /// <summary>
    /// Action to update preferences
    /// </summary>
    public static TsVariableDeclaration UpdatePreferencesAction =>
        Ts.Const<object>("updatePreferences")
            .Value(Ts.Arrow<object, object>("newPrefs")
                .Returns(prefs =>
                    Ts.Var("userStore").Dot("setState").Invoke(
                        Ts.Arrow<object, object>("state").Returns(s =>
                            Ts.Object<object>()
                                .Spread(s)
                                .With("preferences", Ts.Object<object>()
                                    .Spread(s.Prop<object>("preferences"))
                                    .Spread(prefs)
                                    .Build())
                                .Build()
                        )
                    ).Typed<object>()
                ))
            .Export()
            .Build();

    /// <summary>
    /// Selector for checking if user is admin
    /// </summary>
    public static TsVariableDeclaration IsAdminSelector =>
        Ts.Const<object>("selectIsAdmin")
            .Value(Ts.Arrow<object, bool>("state")
                .Returns(s => s.Prop<string[]>("roles").Dot("includes").Invoke(Ts.String("admin")).Typed<bool>()))
            .Export()
            .Build();

    /// <summary>
    /// Hook for using the store in React
    /// </summary>
    public static TsVariableDeclaration UseUserStore =>
        Ts.Const<object>("useUserStore")
            .Value(Ts.Arrow<object>()
                .Returns(StoreExtensions.UseStore(Ts.Var("userStore"))))
            .Export()
            .Build();
}

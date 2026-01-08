namespace Impulse.CodeGen.V2;

public record ImpulseModel(
    IReadOnlyList<EndpointModel> Endpoints,
    IReadOnlyList<MutationModel> Mutations,
    IReadOnlyList<TypeModel> Types,
    IReadOnlyList<ValidatorModel> Validators
);

public record EndpointModel(
    string Route,
    string RouteName,
    string ComponentPath,
    TypeModel PropsType,
    IReadOnlyList<DeferredModel> Deferred,
    IReadOnlyList<LazyModel> Lazy
);

public record MutationModel(
    string Route,
    string RouteName,
    HttpMethod Method,
    TypeModel RequestType,
    TypeModel ResponseType,
    string? HandlerType
);

public record TypeModel(
    string Name,
    TypeKind Kind,
    IReadOnlyList<PropertyModel> Properties,
    IReadOnlyList<string> EnumValues
);

public record PropertyModel(
    string Name,
    string TypeName,
    bool IsOptional,
    bool IsNullable,
    bool IsArray
);

public record ValidatorModel(
    string TypeName,
    IReadOnlyList<PropertyRules> Rules
);

public record PropertyRules(
    string PropertyName,
    IReadOnlyList<ValidationRule> Rules
);

public record ValidationRule(
    RuleKind Kind,
    object? Value
);

public record DeferredModel(string Key, string Url, TypeModel Type);
public record LazyModel(string Key, string Url, TypeModel Type);

public enum TypeKind { Interface, Enum }
public enum HttpMethod { Get, Post, Put, Delete }
public enum RuleKind { NotEmpty, MinLength, MaxLength, Email, GreaterThan, LessThan, Regex }

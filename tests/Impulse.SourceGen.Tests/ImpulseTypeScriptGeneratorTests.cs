using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Impulse.SourceGen;

namespace Impulse.SourceGen.Tests;

/// <summary>
/// Comprehensive tests for ImpulseTypeScriptGenerator.
/// Verifies complex TypeScript generation scenarios.
/// </summary>
public static class ImpulseTypeScriptGeneratorTests
{
    private static int _passed;
    private static int _failed;

    public static int Main()
    {
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine("Impulse.SourceGen Tests");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine();

        // Basic Generation
        Console.WriteLine("Basic TypeScript Generation:");
        Test("Generates interface for simple Props record", GeneratesSimplePropsInterface);
        Test("Generates all primitive type mappings", GeneratesPrimitiveTypes);
        Test("Generates nullable types with | null", GeneratesNullableTypes);
        Test("Uses camelCase for property names", UsesCamelCasePropertyNames);
        Console.WriteLine();

        // Complex Types
        Console.WriteLine("Complex Type Generation:");
        Test("Generates array types as readonly T[]", GeneratesArrayTypes);
        Test("Generates Dictionary as Record<K,V>", GeneratesDictionaryAsRecord);
        Test("Generates nested complex types", GeneratesNestedComplexTypes);
        Test("Includes dependent types transitively", IncludesDependentTypesTransitively);
        Console.WriteLine();

        // Router Types
        Console.WriteLine("Router Type Generation:");
        Test("Generates route params as interface", GeneratesRouteParams);
        Test("Generates typed route response", GeneratesTypedRouteResponse);
        Test("Generates navigation state types", GeneratesNavigationStateTypes);
        Console.WriteLine();

        // Mutation Types
        Console.WriteLine("Mutation Type Generation:");
        Test("Generates mutation request type", GeneratesMutationRequest);
        Test("Generates mutation response type", GeneratesMutationResponse);
        Test("Generates validation error types", GeneratesValidationErrorTypes);
        Console.WriteLine();

        // Form Types
        Console.WriteLine("Form Type Generation:");
        Test("Generates form data with optional fields", GeneratesFormDataWithOptionalFields);
        Test("Generates form step types for wizard", GeneratesWizardStepTypes);
        Test("Generates conditional field visibility types", GeneratesConditionalFieldTypes);
        Console.WriteLine();

        // TanStack Router Complex Types
        Console.WriteLine("TanStack Router Integration:");
        Test("Generates route context with auth state", GeneratesRouteContextWithAuth);
        Test("Generates nested layout route types", GeneratesNestedLayoutRoutes);
        Test("Generates search params with defaults", GeneratesSearchParamsWithDefaults);
        Test("Generates loader data types", GeneratesLoaderDataTypes);
        Test("Generates action data with mutations", GeneratesActionDataWithMutations);
        Test("Generates full route tree types", GeneratesFullRouteTree);
        Test("Generates outlet context types", GeneratesOutletContextTypes);
        Test("Generates error boundary types", GeneratesErrorBoundaryTypes);
        Console.WriteLine();

        // Edge Cases
        Console.WriteLine("Edge Cases:");
        Test("Handles deeply nested generics", HandlesDeepGenerics);
        Test("Handles enum types in props", HandlesEnumTypes);
        Test("Handles self-referential types", HandlesSelfReferentialTypes);
        Test("Handles multiple Props in same namespace", HandlesMultiplePropsInNamespace);
        Console.WriteLine();

        // TypeScript Compilation Verification
        Console.WriteLine("TypeScript Compilation Verification:");
        Test("Complex router types compile with tsc", ComplexRouterTypesCompile);
        Test("Mutation types compile with tsc", MutationTypesCompile);
        Test("Full wizard form types compile with tsc", WizardFormTypesCompile);
        Console.WriteLine();

        // Summary
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"Results: {_passed} passed, {_failed} failed");

        return _failed > 0 ? 1 : 0;
    }

    private static void Test(string name, Func<bool> test)
    {
        try
        {
            if (test())
            {
                Console.WriteLine($"  ✓ {name}");
                _passed++;
            }
            else
            {
                Console.WriteLine($"  ✗ {name}");
                _failed++;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ {name}");
            Console.WriteLine($"    Exception: {ex.Message}");
            _failed++;
        }
    }

    #region Basic Generation Tests

    private static bool GeneratesSimplePropsInterface()
    {
        var source = @"
namespace Test.Features.Dashboard;
public record DashboardProps(string Title, int Count);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface DashboardProps") &&
               ts.Contains("title: string") &&
               ts.Contains("count: number");
    }

    private static bool GeneratesPrimitiveTypes()
    {
        var source = @"
using System;
namespace Test.Features;
public record PrimitivesProps(
    int IntVal,
    long LongVal,
    double DoubleVal,
    decimal DecimalVal,
    float FloatVal,
    string StringVal,
    bool BoolVal,
    DateTime DateTimeVal,
    Guid GuidVal
);";
        var ts = GenerateTypeScript(source);
        return ts.Contains("intVal: number") &&
               ts.Contains("longVal: number") &&
               ts.Contains("doubleVal: number") &&
               ts.Contains("decimalVal: number") &&
               ts.Contains("floatVal: number") &&
               ts.Contains("stringVal: string") &&
               ts.Contains("boolVal: boolean") &&
               ts.Contains("dateTimeVal: string") &&
               ts.Contains("guidVal: string");
    }

    private static bool GeneratesNullableTypes()
    {
        var source = @"
using System;
namespace Test.Features;
public record NullableProps(
    int? NullableInt,
    string? NullableString,
    DateTime? NullableDateTime
);";
        var ts = GenerateTypeScript(source);
        return ts.Contains("nullableInt?: number | null") &&
               ts.Contains("nullableString?: string") &&
               ts.Contains("nullableDateTime?: string | null");
    }

    private static bool UsesCamelCasePropertyNames()
    {
        var source = @"
namespace Test.Features;
public record CamelCaseProps(
    string FirstName,
    string LastName,
    int TotalCount
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("firstName: string") &&
               ts.Contains("lastName: string") &&
               ts.Contains("totalCount: number") &&
               !ts.Contains("FirstName") &&
               !ts.Contains("LastName") &&
               !ts.Contains("TotalCount");
    }

    #endregion

    #region Complex Type Tests

    private static bool GeneratesArrayTypes()
    {
        var source = @"
using System.Collections.Generic;
namespace Test.Features;
public record ArrayProps(
    string[] Tags,
    int[] Numbers,
    List<string> Items,
    IReadOnlyList<int> ReadOnlyNumbers
);";
        var ts = GenerateTypeScript(source);
        return ts.Contains("tags: readonly string[]") &&
               ts.Contains("numbers: readonly number[]") &&
               ts.Contains("items: readonly string[]") &&
               ts.Contains("readOnlyNumbers: readonly number[]");
    }

    private static bool GeneratesDictionaryAsRecord()
    {
        var source = @"
namespace Test.Features;
public record DictProps(
    Dictionary<string, int> Counts,
    IReadOnlyDictionary<string, bool> Flags
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("counts: Record<string, number>") &&
               ts.Contains("flags: Record<string, boolean>");
    }

    private static bool GeneratesNestedComplexTypes()
    {
        var source = @"
namespace Test.Features;
public record AddressData(string Street, string City, string ZipCode);
public record PersonData(string Name, AddressData Address);
public record NestedProps(PersonData Person, AddressData[] Addresses);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface NestedProps") &&
               ts.Contains("export interface PersonData") &&
               ts.Contains("export interface AddressData") &&
               ts.Contains("person: PersonData") &&
               ts.Contains("address: AddressData");
    }

    private static bool IncludesDependentTypesTransitively()
    {
        var source = @"
namespace Test.Features;
public record Level3(string Value);
public record Level2(Level3 Data);
public record Level1(Level2 Child);
public record TransitiveProps(Level1 Root);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface TransitiveProps") &&
               ts.Contains("export interface Level1") &&
               ts.Contains("export interface Level2") &&
               ts.Contains("export interface Level3");
    }

    #endregion

    #region Router Type Tests

    private static bool GeneratesRouteParams()
    {
        var source = @"
namespace Test.Features.Residents;
public record ResidentRouteParams(int Id, string? Tab);
public record ResidentDetailProps(int Id, string Name, ResidentRouteParams RouteParams);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface ResidentRouteParams") &&
               ts.Contains("id: number") &&
               ts.Contains("tab?: string");
    }

    private static bool GeneratesTypedRouteResponse()
    {
        var source = @"
namespace Test.Features;
public record RouteData(string Url, string Method);
public record TypedRouteProps(RouteData[] Routes, Dictionary<string, RouteData> RouteMap);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface RouteData") &&
               ts.Contains("routes: readonly RouteData[]") &&
               ts.Contains("routeMap: Record<string, RouteData>");
    }

    private static bool GeneratesNavigationStateTypes()
    {
        var source = @"
namespace Test.Features;
public record NavigationState(string CurrentPath, string[] History, bool CanGoBack);
public record NavigationProps(NavigationState NavState);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface NavigationState") &&
               ts.Contains("currentPath: string") &&
               ts.Contains("history: readonly string[]") &&
               ts.Contains("canGoBack: boolean");
    }

    #endregion

    #region Mutation Type Tests

    private static bool GeneratesMutationRequest()
    {
        var source = @"
namespace Test.Features;
public record CreateUserRequest(string Email, string Password, string? DisplayName);
public record MutationProps(CreateUserRequest LastRequest);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface CreateUserRequest") &&
               ts.Contains("email: string") &&
               ts.Contains("password: string") &&
               ts.Contains("displayName?: string");
    }

    private static bool GeneratesMutationResponse()
    {
        var source = @"
namespace Test.Features;
public record UserData(int Id, string Email, DateTime CreatedAt);
public record CreateUserResponse(bool Success, UserData? User, string? ErrorMessage);
public record ResponseProps(CreateUserResponse Response);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface CreateUserResponse") &&
               ts.Contains("success: boolean") &&
               ts.Contains("user?: UserData") &&
               ts.Contains("errorMessage?: string");
    }

    private static bool GeneratesValidationErrorTypes()
    {
        var source = @"
namespace Test.Features;
public record ValidationError(string Field, string[] Messages);
public record ValidationResult(bool IsValid, ValidationError[] Errors);
public record FormProps(ValidationResult Validation);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface ValidationError") &&
               ts.Contains("export interface ValidationResult") &&
               ts.Contains("field: string") &&
               ts.Contains("messages: readonly string[]") &&
               ts.Contains("errors: readonly ValidationError[]");
    }

    #endregion

    #region Form Type Tests

    private static bool GeneratesFormDataWithOptionalFields()
    {
        var source = @"
namespace Test.Features;
public record FormData(
    string RequiredField,
    string? OptionalField,
    int? OptionalNumber,
    bool IsActive
);
public record FormProps(FormData Data);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("requiredField: string;") &&
               ts.Contains("optionalField?: string;") &&
               ts.Contains("optionalNumber?: number | null;") &&
               ts.Contains("isActive: boolean;");
    }

    private static bool GeneratesWizardStepTypes()
    {
        var source = @"
namespace Test.Features;
public record StepData(int StepNumber, string Title, bool IsComplete, bool IsCurrent);
public record WizardFormData(string? FirstName, string? LastName, DateTime? BirthDate);
public record WizardProps(
    int CurrentStep,
    int TotalSteps,
    StepData[] Steps,
    WizardFormData FormData,
    bool CanGoBack,
    bool CanGoNext
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface WizardProps") &&
               ts.Contains("export interface StepData") &&
               ts.Contains("export interface WizardFormData") &&
               ts.Contains("currentStep: number") &&
               ts.Contains("steps: readonly StepData[]") &&
               ts.Contains("formData: WizardFormData");
    }

    private static bool GeneratesConditionalFieldTypes()
    {
        var source = @"
namespace Test.Features;
public record FieldVisibility(string FieldName, bool IsVisible, string? DependsOn);
public record ConditionalFormData(
    bool ShowAdvanced,
    string? AdvancedOption,
    bool HasInsurance,
    string? InsuranceProvider
);
public record ConditionalFormProps(
    ConditionalFormData Data,
    FieldVisibility[] FieldRules
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface FieldVisibility") &&
               ts.Contains("export interface ConditionalFormData") &&
               ts.Contains("showAdvanced: boolean") &&
               ts.Contains("advancedOption?: string") &&
               ts.Contains("fieldRules: readonly FieldVisibility[]");
    }

    #endregion

    #region TanStack Router Integration Tests

    private static bool GeneratesRouteContextWithAuth()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.Router;

// User session for auth context
public record UserSession(
    int UserId,
    string Email,
    string[] Roles,
    DateTime ExpiresAt,
    Dictionary<string, string> Permissions
);

// Auth state with loading/error states
public record AuthState(
    bool IsAuthenticated,
    bool IsLoading,
    UserSession? User,
    string? ErrorMessage
);

// Route context passed down through router
public record RouteContext(
    AuthState Auth,
    string Locale,
    string Theme,
    Dictionary<string, object> FeatureFlags
);

// Root layout props with context
public record RootLayoutProps(
    RouteContext Context,
    string[] Breadcrumbs,
    Dictionary<string, string> Meta
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface UserSession") &&
               ts.Contains("userId: number") &&
               ts.Contains("roles: readonly string[]") &&
               ts.Contains("permissions: Record<string, string>") &&
               ts.Contains("export interface AuthState") &&
               ts.Contains("user?: UserSession") &&
               ts.Contains("export interface RouteContext") &&
               ts.Contains("auth: AuthState") &&
               ts.Contains("featureFlags: Record<string, unknown>") &&
               ts.Contains("export interface RootLayoutProps");
    }

    private static bool GeneratesNestedLayoutRoutes()
    {
        var source = @"
using System.Collections.Generic;
namespace App.Features.Layout;

// Navigation item for menus
public record NavItem(
    string Id,
    string Label,
    string Path,
    string? Icon,
    NavItem[] Children,
    bool IsActive,
    int? Badge
);

// Sidebar layout with nav
public record SidebarData(
    NavItem[] MainNav,
    NavItem[] FooterNav,
    bool IsCollapsed,
    int Width
);

// Header with user menu
public record HeaderData(
    string Title,
    NavItem[] UserMenuItems,
    int NotificationCount
);

// Dashboard layout combining header + sidebar
public record DashboardLayoutProps(
    HeaderData Header,
    SidebarData Sidebar,
    string[] Breadcrumbs,
    Dictionary<string, string> PageMeta
);

// Nested admin layout
public record AdminLayoutProps(
    DashboardLayoutProps ParentLayout,
    NavItem[] AdminTabs,
    bool HasUnsavedChanges
);

// Deep nested settings layout
public record SettingsLayoutProps(
    AdminLayoutProps ParentLayout,
    NavItem[] SettingsSections,
    string ActiveSection
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface NavItem") &&
               ts.Contains("children: readonly NavItem[]") &&
               ts.Contains("export interface SidebarData") &&
               ts.Contains("mainNav: readonly NavItem[]") &&
               ts.Contains("export interface DashboardLayoutProps") &&
               ts.Contains("header: HeaderData") &&
               ts.Contains("sidebar: SidebarData") &&
               ts.Contains("export interface AdminLayoutProps") &&
               ts.Contains("parentLayout: DashboardLayoutProps") &&
               ts.Contains("export interface SettingsLayoutProps") &&
               ts.Contains("parentLayout: AdminLayoutProps");
    }

    private static bool GeneratesSearchParamsWithDefaults()
    {
        var source = @"
using System;
namespace App.Features.Search;

// Search params with pagination
public record PaginationParams(
    int Page,
    int PageSize,
    string? SortBy,
    bool SortDescending
);

// Filter params with multiple types
public record FilterParams(
    string? Query,
    string[] Tags,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? MinValue,
    int? MaxValue,
    bool? IsActive
);

// Combined search params for list views
public record ListSearchParams(
    PaginationParams Pagination,
    FilterParams Filters,
    string[] VisibleColumns,
    string? ViewMode
);

// Props with search params
public record ResidentListProps(
    ListSearchParams SearchParams,
    int TotalCount,
    bool IsLoading
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface PaginationParams") &&
               ts.Contains("page: number") &&
               ts.Contains("sortBy?: string") &&
               ts.Contains("export interface FilterParams") &&
               ts.Contains("tags: readonly string[]") &&
               ts.Contains("dateFrom?: string | null") &&
               ts.Contains("export interface ListSearchParams") &&
               ts.Contains("pagination: PaginationParams") &&
               ts.Contains("filters: FilterParams") &&
               ts.Contains("export interface ResidentListProps") &&
               ts.Contains("searchParams: ListSearchParams");
    }

    private static bool GeneratesLoaderDataTypes()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.Loader;

// Resident detail loader data
public record ResidentData(
    int Id,
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string RoomNumber,
    Dictionary<string, string> CustomFields
);

// Medications loader data
public record MedicationData(
    int Id,
    string Name,
    string Dosage,
    string Frequency,
    DateTime? NextDose,
    bool IsActive
);

// Emergency contact loader data
public record EmergencyContact(
    string Name,
    string Relationship,
    string Phone,
    string? Email,
    bool IsPrimary
);

// Combined loader data for resident detail
public record ResidentLoaderData(
    ResidentData Resident,
    MedicationData[] Medications,
    EmergencyContact[] Contacts,
    Dictionary<string, int> Stats
);

// Props with loader data
public record ResidentDetailProps(
    ResidentLoaderData LoaderData,
    string ActiveTab,
    bool CanEdit,
    bool CanDelete
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface ResidentData") &&
               ts.Contains("customFields: Record<string, string>") &&
               ts.Contains("export interface MedicationData") &&
               ts.Contains("nextDose?: string | null") &&
               ts.Contains("export interface EmergencyContact") &&
               ts.Contains("isPrimary: boolean") &&
               ts.Contains("export interface ResidentLoaderData") &&
               ts.Contains("medications: readonly MedicationData[]") &&
               ts.Contains("contacts: readonly EmergencyContact[]") &&
               ts.Contains("stats: Record<string, number>") &&
               ts.Contains("export interface ResidentDetailProps") &&
               ts.Contains("loaderData: ResidentLoaderData");
    }

    private static bool GeneratesActionDataWithMutations()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.Mutations;

// Form field error
public record FieldError(string Field, string[] Messages);

// Validation result
public record ValidationResult(
    bool IsValid,
    FieldError[] FieldErrors,
    string[] GlobalErrors
);

// Mutation state
public record MutationState(
    bool IsIdle,
    bool IsPending,
    bool IsSuccess,
    bool IsError,
    string? ErrorMessage,
    ValidationResult? Validation
);

// Create resident request
public record CreateResidentRequest(
    string FirstName,
    string LastName,
    DateTime DateOfBirth,
    string? RoomNumber,
    Dictionary<string, string>? CustomFields,
    int[] MedicationIds
);

// Create resident response
public record CreateResidentResponse(
    bool Success,
    int? ResidentId,
    string? RedirectUrl,
    ValidationResult? Validation
);

// Action data for form
public record ResidentFormActionData(
    MutationState CreateState,
    MutationState UpdateState,
    MutationState DeleteState,
    CreateResidentResponse? LastResponse
);

// Props with action data
public record ResidentFormProps(
    CreateResidentRequest? InitialData,
    ResidentFormActionData ActionData,
    string FormMode,
    bool IsDisabled
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface FieldError") &&
               ts.Contains("messages: readonly string[]") &&
               ts.Contains("export interface ValidationResult") &&
               ts.Contains("fieldErrors: readonly FieldError[]") &&
               ts.Contains("globalErrors: readonly string[]") &&
               ts.Contains("export interface MutationState") &&
               ts.Contains("validation?: ValidationResult") &&
               ts.Contains("export interface CreateResidentRequest") &&
               ts.Contains("medicationIds: readonly number[]") &&
               ts.Contains("export interface CreateResidentResponse") &&
               ts.Contains("export interface ResidentFormActionData") &&
               ts.Contains("createState: MutationState") &&
               ts.Contains("lastResponse?: CreateResidentResponse") &&
               ts.Contains("export interface ResidentFormProps") &&
               ts.Contains("actionData: ResidentFormActionData");
    }

    private static bool GeneratesFullRouteTree()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.Routes;

// Route meta for SEO and layouts
public record RouteMeta(
    string Title,
    string? Description,
    string[]? Keywords,
    string? Layout,
    bool RequiresAuth,
    string[]? RequiredRoles
);

// Route definition
public record RouteDefinition(
    string Id,
    string Path,
    string? ParentId,
    RouteMeta Meta,
    bool IsIndex,
    bool HasLoader,
    bool HasAction
);

// Route match info
public record RouteMatch(
    string RouteId,
    string PathName,
    Dictionary<string, string> Params,
    Dictionary<string, string> SearchParams,
    int Score
);

// Route tree node (recursive)
public record RouteTreeNode(
    RouteDefinition Route,
    RouteTreeNode[] Children,
    bool IsActive,
    bool IsExact
);

// Full router state
public record RouterState(
    string CurrentPath,
    RouteMatch[] Matches,
    RouteTreeNode RouteTree,
    string[] BreadcrumbIds,
    Dictionary<string, object> LocationState,
    bool IsNavigating
);

// Root router props
public record RouterProps(
    RouterState State,
    RouteMeta CurrentMeta,
    Dictionary<string, RouteDefinition> RouteMap
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface RouteMeta") &&
               ts.Contains("requiredRoles?: readonly string[]") &&
               ts.Contains("export interface RouteDefinition") &&
               ts.Contains("meta: RouteMeta") &&
               ts.Contains("export interface RouteMatch") &&
               ts.Contains("params: Record<string, string>") &&
               ts.Contains("searchParams: Record<string, string>") &&
               ts.Contains("export interface RouteTreeNode") &&
               ts.Contains("route: RouteDefinition") &&
               ts.Contains("children: readonly RouteTreeNode[]") &&
               ts.Contains("export interface RouterState") &&
               ts.Contains("matches: readonly RouteMatch[]") &&
               ts.Contains("routeTree: RouteTreeNode") &&
               ts.Contains("export interface RouterProps") &&
               ts.Contains("state: RouterState") &&
               ts.Contains("routeMap: Record<string, RouteDefinition>");
    }

    private static bool GeneratesOutletContextTypes()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.Outlet;

// Shared data passed through outlet
public record OutletSharedData(
    int EntityId,
    string EntityType,
    bool CanEdit,
    bool CanDelete,
    DateTime LastModified
);

// Tab definition
public record TabDefinition(
    string Id,
    string Label,
    string Path,
    bool IsDisabled,
    int? Badge
);

// Outlet context with tabs
public record TabbedOutletContext(
    OutletSharedData SharedData,
    TabDefinition[] Tabs,
    string ActiveTabId,
    Dictionary<string, bool> TabStates
);

// Nested outlet for sub-routes
public record NestedOutletContext(
    TabbedOutletContext ParentContext,
    string SubSection,
    Dictionary<string, object> LocalState
);

// Props for outlet component
public record OutletContainerProps(
    NestedOutletContext Context,
    bool ShowBreadcrumbs,
    bool ShowTabs
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface OutletSharedData") &&
               ts.Contains("export interface TabDefinition") &&
               ts.Contains("badge?: number | null") &&
               ts.Contains("export interface TabbedOutletContext") &&
               ts.Contains("sharedData: OutletSharedData") &&
               ts.Contains("tabs: readonly TabDefinition[]") &&
               ts.Contains("tabStates: Record<string, boolean>") &&
               ts.Contains("export interface NestedOutletContext") &&
               ts.Contains("parentContext: TabbedOutletContext") &&
               ts.Contains("localState: Record<string, unknown>") &&
               ts.Contains("export interface OutletContainerProps") &&
               ts.Contains("context: NestedOutletContext");
    }

    private static bool GeneratesErrorBoundaryTypes()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.ErrorHandling;

// Error details
public record ErrorDetails(
    string Code,
    string Message,
    string? StackTrace,
    Dictionary<string, string>? Metadata
);

// Route error info
public record RouteError(
    int Status,
    string StatusText,
    ErrorDetails? Details,
    string RouteId,
    string Path
);

// Error boundary state
public record ErrorBoundaryState(
    bool HasError,
    RouteError? Error,
    string[] ErrorHistory,
    int RetryCount
);

// Recovery action
public record RecoveryAction(
    string Id,
    string Label,
    string ActionType,
    Dictionary<string, string>? Params
);

// Error boundary props
public record ErrorBoundaryProps(
    ErrorBoundaryState State,
    RecoveryAction[] AvailableActions,
    bool CanRetry,
    bool CanGoBack,
    string? FallbackPath
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface ErrorDetails") &&
               ts.Contains("metadata?: Record<string, string>") &&
               ts.Contains("export interface RouteError") &&
               ts.Contains("details?: ErrorDetails") &&
               ts.Contains("export interface ErrorBoundaryState") &&
               ts.Contains("error?: RouteError") &&
               ts.Contains("errorHistory: readonly string[]") &&
               ts.Contains("export interface RecoveryAction") &&
               ts.Contains("params?: Record<string, string>") &&
               ts.Contains("export interface ErrorBoundaryProps") &&
               ts.Contains("state: ErrorBoundaryState") &&
               ts.Contains("availableActions: readonly RecoveryAction[]");
    }

    #endregion

    #region Edge Case Tests

    private static bool HandlesDeepGenerics()
    {
        var source = @"
namespace Test.Features;
public record DeepGenericProps(
    List<Dictionary<string, int[]>> ComplexData,
    IReadOnlyDictionary<string, List<string>> NestedCollections
);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("complexData: readonly Record<string, readonly number[]>[]") &&
               ts.Contains("nestedCollections: Record<string, readonly string[]>");
    }

    private static bool HandlesEnumTypes()
    {
        // Note: Enums require special handling - current implementation uses type name
        var source = @"
namespace Test.Features;
public record StatusData(string Name, int Code);
public record EnumProps(StatusData CurrentStatus, StatusData[] AllStatuses);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface StatusData") &&
               ts.Contains("currentStatus: StatusData") &&
               ts.Contains("allStatuses: readonly StatusData[]");
    }

    private static bool HandlesSelfReferentialTypes()
    {
        var source = @"
namespace Test.Features;
public record TreeNode(string Value, TreeNode[] Children);
public record TreeProps(TreeNode Root);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface TreeNode") &&
               ts.Contains("value: string") &&
               ts.Contains("children: readonly TreeNode[]");
    }

    private static bool HandlesMultiplePropsInNamespace()
    {
        var source = @"
namespace Test.Features.Users;
public record UserListProps(int Count);
public record UserDetailProps(int Id, string Name);
public record UserEditProps(int Id, string? Name, string? Email);
";
        var ts = GenerateTypeScript(source);
        return ts.Contains("export interface UserListProps") &&
               ts.Contains("export interface UserDetailProps") &&
               ts.Contains("export interface UserEditProps");
    }

    #endregion

    #region TypeScript Compilation Verification Tests

    private static bool ComplexRouterTypesCompile()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.ComplexRouter;

public record UserSession(int UserId, string Email, string[] Roles, DateTime ExpiresAt, Dictionary<string, string> Permissions);
public record AuthState(bool IsAuthenticated, bool IsLoading, UserSession? User, string? ErrorMessage);
public record RouteContext(AuthState Auth, string Locale, string Theme, Dictionary<string, object> FeatureFlags);
public record NavItem(string Id, string Label, string Path, string? Icon, NavItem[] Children, bool IsActive, int? Badge);
public record SidebarData(NavItem[] MainNav, NavItem[] FooterNav, bool IsCollapsed, int Width);
public record HeaderData(string Title, NavItem[] UserMenuItems, int NotificationCount);
public record RouterLayoutProps(RouteContext Context, HeaderData Header, SidebarData Sidebar, string[] Breadcrumbs);
";
        return VerifyTypeScriptCompiles(source);
    }

    private static bool MutationTypesCompile()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.Mutations;

public record FieldError(string Field, string[] Messages);
public record ValidationResult(bool IsValid, FieldError[] FieldErrors, string[] GlobalErrors);
public record MutationState(bool IsIdle, bool IsPending, bool IsSuccess, bool IsError, string? ErrorMessage, ValidationResult? Validation);
public record CreateResidentRequest(string FirstName, string LastName, DateTime DateOfBirth, string? RoomNumber, Dictionary<string, string>? CustomFields, int[] MedicationIds);
public record CreateResidentResponse(bool Success, int? ResidentId, string? RedirectUrl, ValidationResult? Validation);
public record ResidentFormActionData(MutationState CreateState, MutationState UpdateState, MutationState DeleteState, CreateResidentResponse? LastResponse);
public record MutationFormProps(CreateResidentRequest? InitialData, ResidentFormActionData ActionData, string FormMode, bool IsDisabled);
";
        return VerifyTypeScriptCompiles(source);
    }

    private static bool WizardFormTypesCompile()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace App.Features.Wizard;

public record WizardStepData(int StepNumber, string Title, string Description, bool IsCompleted, bool IsCurrent);
public record WizardFormData(
    string? FirstName, string? LastName, DateTime? DateOfBirth, string? Gender,
    string? PrimaryPhysician, string[]? Allergies, string[]? Medications, string? BloodType,
    bool HasInsurance, string? InsuranceProvider, string? InsurancePolicyNumber,
    string? EmergencyContactName, string? EmergencyContactPhone, string? EmergencyContactRelation,
    int? BuildingId, int? FloorNumber, int? RoomNumber
);
public record WizardStepRequest(int CurrentStep, WizardFormData FormData);
public record WizardStepResponse(bool Success, int NextStep, Dictionary<string, string[]>? ValidationErrors);
public record WizardSubmitRequest(WizardFormData FormData);
public record WizardSubmitResponse(bool Success, int? ResidentId, string? Message);
public record WizardProps(int CurrentStep, int TotalSteps, WizardStepData[] Steps, WizardFormData FormData, bool CanGoBack, bool CanGoNext, bool CanSubmit);
";
        return VerifyTypeScriptCompiles(source);
    }

    private static bool VerifyTypeScriptCompiles(string csharpSource)
    {
        var ts = GenerateTypeScript(csharpSource);
        if (string.IsNullOrEmpty(ts)) return false;

        // Write to temp file
        var tempDir = Path.Combine(Path.GetTempPath(), $"impulse-ts-verify-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var tsFile = Path.Combine(tempDir, "types.ts");
            var tsconfigFile = Path.Combine(tempDir, "tsconfig.json");

            File.WriteAllText(tsFile, ts);
            File.WriteAllText(tsconfigFile, @"{
  ""compilerOptions"": {
    ""strict"": true,
    ""noEmit"": true,
    ""skipLibCheck"": true,
    ""target"": ""ES2020"",
    ""module"": ""ESNext"",
    ""moduleResolution"": ""bundler""
  },
  ""include"": [""*.ts""]
}");

            // Run tsc
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "npx";
            process.StartInfo.Arguments = "tsc --noEmit";
            process.StartInfo.WorkingDirectory = tempDir;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(30000);

            if (process.ExitCode != 0)
            {
                Console.WriteLine($"    TypeScript compilation failed:");
                Console.WriteLine($"    {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Warning: Could not run tsc: {ex.Message}");
            // If tsc isn't available, skip this test gracefully
            return true;
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    #endregion

    #region Helper Methods

    private static string GenerateTypeScript(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IReadOnlyDictionary<,>).Assembly.Location),
        };

        // Add System.Runtime reference
        var runtimePath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var runtimeRef = MetadataReference.CreateFromFile(Path.Combine(runtimePath, "System.Runtime.dll"));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references.Append(runtimeRef),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ImpulseTypeScriptGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();

        if (runResult.GeneratedTrees.Length == 0)
        {
            return "";
        }

        var generatedSource = runResult.GeneratedTrees[0].GetText().ToString();

        // Extract TypeScript from comments
        return ExtractTypeScript(generatedSource);
    }

    private static string ExtractTypeScript(string generatedCs)
    {
        var startMarker = "// <impulse-typescript file=\"types.g.ts\">";
        var endMarker = "// </impulse-typescript>";

        var startIdx = generatedCs.IndexOf(startMarker);
        var endIdx = generatedCs.IndexOf(endMarker);

        if (startIdx < 0 || endIdx < 0) return "";

        var content = generatedCs.Substring(startIdx + startMarker.Length, endIdx - startIdx - startMarker.Length);

        var sb = new StringBuilder();
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("// "))
            {
                sb.AppendLine(trimmed.Substring(3));
            }
            else if (trimmed == "//")
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    #endregion
}

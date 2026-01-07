using Impulse.Core;

namespace Impulse.Core.Tests;

/// <summary>
/// TDD Level 2-4: Tests for Deferred, Lazy, and Mutation endpoint configuration.
/// </summary>
public static class ImpulseEndpointTests
{
    public static void RunAll()
    {
        TestRunner.Group("ImpulseComponentMetadata - Deferred (Level 2)", () =>
        {
            TestRunner.Run("Deferred adds URL to metadata",
                Deferred_AddsUrlToMetadata);

            TestRunner.Run("Deferred stores props type",
                Deferred_StoresPropsType);

            TestRunner.Run("Multiple deferred can be registered",
                Multiple_Deferred_CanBeRegistered);

            TestRunner.Run("Deferred URL template with parameters",
                Deferred_UrlTemplateWithParameters);
        });

        TestRunner.Group("ImpulseComponentMetadata - Lazy (Level 3)", () =>
        {
            TestRunner.Run("Lazy adds URL to metadata",
                Lazy_AddsUrlToMetadata);

            TestRunner.Run("Lazy stores props type",
                Lazy_StoresPropsType);

            TestRunner.Run("Multiple lazy can be registered",
                Multiple_Lazy_CanBeRegistered);

            TestRunner.Run("Lazy URL template with route parameters",
                Lazy_UrlTemplateWithRouteParameters);
        });

        TestRunner.Group("ImpulseMutationMetadata - Mutations (Level 4)", () =>
        {
            TestRunner.Run("Mutation stores request type",
                Mutation_StoresRequestType);

            TestRunner.Run("Mutation stores response type",
                Mutation_StoresResponseType);

            TestRunner.Run("Mutation can declare invalidations",
                Mutation_CanDeclareInvalidations);

            TestRunner.Run("Mutation can invalidate multiple types",
                Mutation_CanInvalidateMultipleTypes);
        });

        TestRunner.Group("ImpulsePayload - URL Generation", () =>
        {
            TestRunner.Run("Payload with deferred generates correct structure",
                Payload_WithDeferred_GeneratesCorrectStructure);

            TestRunner.Run("Payload with lazy generates correct structure",
                Payload_WithLazy_GeneratesCorrectStructure);

            TestRunner.Run("Payload can have both deferred and lazy",
                Payload_CanHaveBothDeferredAndLazy);
        });
    }

    // ═══════════════════════════════════════════════════════
    // Deferred Tests (Level 2)
    // ═══════════════════════════════════════════════════════

    private static void Deferred_AddsUrlToMetadata()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardProps),
            ComponentPath = "./Dashboard"
        };

        metadata.Deferred["stats"] = new DeferredComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardStatsProps),
            UrlTemplate = "/api/dashboard/stats"
        };

        Assert.True(metadata.Deferred.ContainsKey("stats"));
        Assert.Equal("/api/dashboard/stats", metadata.Deferred["stats"].UrlTemplate);
    }

    private static void Deferred_StoresPropsType()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardProps),
            ComponentPath = "./Dashboard"
        };

        metadata.Deferred["stats"] = new DeferredComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardStatsProps),
            UrlTemplate = "/api/dashboard/stats"
        };

        Assert.Equal(typeof(TestTypes.Features.Dashboard.DashboardStatsProps),
            metadata.Deferred["stats"].PropsType);
    }

    private static void Multiple_Deferred_CanBeRegistered()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardProps),
            ComponentPath = "./Dashboard"
        };

        metadata.Deferred["stats"] = new DeferredComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardStatsProps),
            UrlTemplate = "/api/dashboard/stats"
        };

        metadata.Deferred["notifications"] = new DeferredComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Dashboard.NotificationsProps),
            UrlTemplate = "/api/notifications"
        };

        Assert.Equal(2, metadata.Deferred.Count);
        Assert.True(metadata.Deferred.ContainsKey("stats"));
        Assert.True(metadata.Deferred.ContainsKey("notifications"));
    }

    private static void Deferred_UrlTemplateWithParameters()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardProps),
            ComponentPath = "./Dashboard"
        };

        metadata.Deferred["userStats"] = new DeferredComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Dashboard.DashboardStatsProps),
            UrlTemplate = "/api/users/{userId}/stats"
        };

        Assert.Contains("{userId}", metadata.Deferred["userStats"].UrlTemplate);
    }

    // ═══════════════════════════════════════════════════════
    // Lazy Tests (Level 3)
    // ═══════════════════════════════════════════════════════

    private static void Lazy_AddsUrlToMetadata()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentsListProps),
            ComponentPath = "./Residents/List"
        };

        metadata.Lazy["details"] = new LazyComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentDetailProps),
            UrlTemplate = "/api/residents/{id}"
        };

        Assert.True(metadata.Lazy.ContainsKey("details"));
        Assert.Equal("/api/residents/{id}", metadata.Lazy["details"].UrlTemplate);
    }

    private static void Lazy_StoresPropsType()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentsListProps),
            ComponentPath = "./Residents/List"
        };

        metadata.Lazy["details"] = new LazyComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentDetailProps),
            UrlTemplate = "/api/residents/{id}"
        };

        Assert.Equal(typeof(TestTypes.Features.Residents.ResidentDetailProps),
            metadata.Lazy["details"].PropsType);
    }

    private static void Multiple_Lazy_CanBeRegistered()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentsListProps),
            ComponentPath = "./Residents/List"
        };

        metadata.Lazy["details"] = new LazyComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentDetailProps),
            UrlTemplate = "/api/residents/{id}"
        };

        metadata.Lazy["history"] = new LazyComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentHistoryProps),
            UrlTemplate = "/api/residents/{id}/history"
        };

        Assert.Equal(2, metadata.Lazy.Count);
        Assert.True(metadata.Lazy.ContainsKey("details"));
        Assert.True(metadata.Lazy.ContainsKey("history"));
    }

    private static void Lazy_UrlTemplateWithRouteParameters()
    {
        var metadata = new ImpulseComponentMetadata
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentsListProps),
            ComponentPath = "./Residents/List"
        };

        metadata.Lazy["apartment"] = new LazyComponentInfo
        {
            PropsType = typeof(TestTypes.Features.Residents.ResidentDetailProps),
            UrlTemplate = "/api/buildings/{buildingId}/apartments/{apartmentId}"
        };

        Assert.Contains("{buildingId}", metadata.Lazy["apartment"].UrlTemplate);
        Assert.Contains("{apartmentId}", metadata.Lazy["apartment"].UrlTemplate);
    }

    // ═══════════════════════════════════════════════════════
    // Mutation Tests (Level 4)
    // ═══════════════════════════════════════════════════════

    private static void Mutation_StoresRequestType()
    {
        var metadata = new ImpulseMutationMetadata
        {
            RequestType = typeof(TestTypes.CreateResidentRequest),
            ResponseType = typeof(TestTypes.CreateResidentResponse)
        };

        Assert.Equal(typeof(TestTypes.CreateResidentRequest), metadata.RequestType);
    }

    private static void Mutation_StoresResponseType()
    {
        var metadata = new ImpulseMutationMetadata
        {
            RequestType = typeof(TestTypes.CreateResidentRequest),
            ResponseType = typeof(TestTypes.CreateResidentResponse)
        };

        Assert.Equal(typeof(TestTypes.CreateResidentResponse), metadata.ResponseType);
    }

    private static void Mutation_CanDeclareInvalidations()
    {
        var metadata = new ImpulseMutationMetadata
        {
            RequestType = typeof(TestTypes.CreateResidentRequest),
            ResponseType = typeof(TestTypes.CreateResidentResponse)
        };

        metadata.InvalidatesTypes.Add(typeof(TestTypes.Features.Residents.ResidentsListProps));

        Assert.Equal(1, metadata.InvalidatesTypes.Count);
        Assert.True(metadata.InvalidatesTypes.Contains(typeof(TestTypes.Features.Residents.ResidentsListProps)));
    }

    private static void Mutation_CanInvalidateMultipleTypes()
    {
        var metadata = new ImpulseMutationMetadata
        {
            RequestType = typeof(TestTypes.UpdateResidentRequest),
            ResponseType = typeof(TestTypes.UpdateResidentResponse)
        };

        metadata.InvalidatesTypes.Add(typeof(TestTypes.Features.Residents.ResidentsListProps));
        metadata.InvalidatesTypes.Add(typeof(TestTypes.Features.Residents.ResidentDetailProps));

        Assert.Equal(2, metadata.InvalidatesTypes.Count);
        Assert.True(metadata.InvalidatesTypes.Contains(typeof(TestTypes.Features.Residents.ResidentsListProps)));
        Assert.True(metadata.InvalidatesTypes.Contains(typeof(TestTypes.Features.Residents.ResidentDetailProps)));
    }

    // ═══════════════════════════════════════════════════════
    // Payload URL Generation Tests
    // ═══════════════════════════════════════════════════════

    private static void Payload_WithDeferred_GeneratesCorrectStructure()
    {
        var payload = new ImpulsePayload
        {
            Url = "/dashboard",
            Version = "1.0.0",
            Props = new { Title = "Dashboard" },
            Context = new { },
            Deferred = new Dictionary<string, string>
            {
                ["stats"] = "/api/dashboard/stats",
                ["notifications"] = "/api/notifications"
            }
        };

        Assert.NotNull(payload.Deferred);
        Assert.Equal(2, payload.Deferred.Count);
        Assert.Equal("/api/dashboard/stats", payload.Deferred["stats"]);
        Assert.Equal("/api/notifications", payload.Deferred["notifications"]);
    }

    private static void Payload_WithLazy_GeneratesCorrectStructure()
    {
        var payload = new ImpulsePayload
        {
            Url = "/residents",
            Version = "1.0.0",
            Props = new { Items = new[] { 1, 2, 3 } },
            Context = new { },
            Lazy = new Dictionary<string, string>
            {
                ["details"] = "/api/residents/{id}"
            }
        };

        Assert.NotNull(payload.Lazy);
        Assert.Equal(1, payload.Lazy.Count);
        Assert.Equal("/api/residents/{id}", payload.Lazy["details"]);
    }

    private static void Payload_CanHaveBothDeferredAndLazy()
    {
        var payload = new ImpulsePayload
        {
            Url = "/dashboard",
            Version = "1.0.0",
            Props = new { },
            Context = new { },
            Deferred = new Dictionary<string, string>
            {
                ["stats"] = "/api/stats"
            },
            Lazy = new Dictionary<string, string>
            {
                ["details"] = "/api/details/{id}"
            }
        };

        Assert.NotNull(payload.Deferred);
        Assert.NotNull(payload.Lazy);
        Assert.Equal(1, payload.Deferred.Count);
        Assert.Equal(1, payload.Lazy.Count);
    }
}

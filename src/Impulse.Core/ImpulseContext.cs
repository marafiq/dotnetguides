using Microsoft.AspNetCore.Http;

namespace Impulse.Core;

/// <summary>
/// Interface for providing the app-wide context.
/// </summary>
public interface IImpulseContextProvider
{
    Task<object> GetContextAsync(HttpContext httpContext);
}

/// <summary>
/// Typed context provider.
/// </summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IImpulseContextProvider<TContext> : IImpulseContextProvider
{
    new Task<TContext> GetContextAsync(HttpContext httpContext);
}

/// <summary>
/// Implementation of the typed context provider.
/// </summary>
internal sealed class ImpulseContextProvider<TContext> : IImpulseContextProvider<TContext>
{
    private readonly Func<HttpContext, Task<TContext>> _contextFactory;

    public ImpulseContextProvider(Func<HttpContext, Task<TContext>> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<TContext> GetContextAsync(HttpContext httpContext)
    {
        return await _contextFactory(httpContext);
    }

    async Task<object> IImpulseContextProvider.GetContextAsync(HttpContext httpContext)
    {
        var context = await GetContextAsync(httpContext);
        return context!;
    }
}

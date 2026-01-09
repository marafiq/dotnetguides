namespace Impulse.Core;

/// <summary>
/// Base class for Impulse endpoints.
/// Endpoints handle both HTML and JSON responses based on X-Impulse header.
/// </summary>
/// <typeparam name="TRequest">The request/input type</typeparam>
/// <typeparam name="TResponse">The response/output type</typeparam>
public abstract class ImpulseEndpoint<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Handle the request and return a result.
    /// </summary>
    /// <param name="request">The validated request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An Impulse result that will be rendered as HTML or JSON</returns>
    public abstract Task<IImpulseResult> Handle(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for Impulse endpoints without a request body (GET endpoints).
/// </summary>
/// <typeparam name="TResponse">The response/output type</typeparam>
public abstract class ImpulseEndpoint<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Handle the request and return a result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An Impulse result that will be rendered as HTML or JSON</returns>
    public abstract Task<IImpulseResult> Handle(CancellationToken cancellationToken = default);
}

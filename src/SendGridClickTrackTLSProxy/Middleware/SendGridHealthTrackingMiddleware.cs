
using SendGridClickTrackTLSProxy.HealthChecks;
using SendGridClickTrackTLSProxy.Services;
using Serilog;
namespace SendGridClickTrackTLSProxy.Middleware;

public class SendGridHealthTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SendGridHealthTrackingConfigService _config;

    public SendGridHealthTrackingMiddleware(RequestDelegate next, SendGridHealthTrackingConfigService config)
    {
        _next = next;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context, SendGridHealthTracker healthTracker)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Don't track cancelled requests in health metrics
            Log.Debug("Proxy request cancelled: {Path}", context.Request.Path);
            return;
        }

        if (ShouldTrackRequest(context))
        {
            TrackRequestHealth(context, healthTracker);
        }
    }

    private bool ShouldTrackRequest(HttpContext context)
    {
        return IsGetRequest(context) &&
               IsProxyHostRequest(context) &&
               IsTrackingPathRequest(context);
    }

    private bool IsGetRequest(HttpContext context)
    {
        return string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsProxyHostRequest(HttpContext context)
    {
        return context.Request.Host.Host.Equals(_config.ProxyHost, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsTrackingPathRequest(HttpContext context)
    {
        return _config.TrackingPathPrefixes.Any(prefix =>
            context.Request.Path.StartsWithSegments(prefix));
    }

    private void TrackRequestHealth(HttpContext context, SendGridHealthTracker healthTracker)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            Log.Debug("SendGrid proxy request was cancelled: {Path}", context.Request.Path);
            return;
        }

        healthTracker.RecordResponse(context.Response.StatusCode);
        Log.Information("SendGrid proxy response: {StatusCode} for {Method} {Path}",
            context.Response.StatusCode, context.Request.Method, context.Request.Path);
    }
}
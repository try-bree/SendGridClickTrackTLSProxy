namespace SendGridClickTrackTLSProxy.Middleware;

public class HostHeaderValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HostHeaderValidationMiddleware> _logger;
    private readonly string _allowedHost;

    public HostHeaderValidationMiddleware(
        RequestDelegate next,
        ILogger<HostHeaderValidationMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _allowedHost = configuration["SendGrid:ClickTrackingCustomDomain"]
            ?? throw new ArgumentException("SendGrid:ClickTrackingCustomDomain configuration is required");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestHost = context.Request.Host.Host;

        // Reject requests that don't match our allowed host
        if (!string.Equals(requestHost, _allowedHost, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Rejected request for invalid host '{RequestHost}'. Expected: '{AllowedHost}'. " +
                "Path: {Path}, Method: {Method}, UserAgent: {UserAgent}, RemoteIP: {RemoteIP}",
                requestHost,
                _allowedHost,
                context.Request.Path,
                context.Request.Method,
                context.Request.Headers.UserAgent.ToString(),
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"Host '{requestHost}' not found");
            return;
        }

        await _next(context);
    }
}
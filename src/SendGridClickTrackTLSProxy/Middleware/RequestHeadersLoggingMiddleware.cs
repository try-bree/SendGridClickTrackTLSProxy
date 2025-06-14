using Serilog;
using ILogger = Serilog.ILogger;

namespace SendGridClickTrackTLSProxy.Middleware;

public class RequestHeadersLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public RequestHeadersLoggingMiddleware(RequestDelegate next, ILogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Request.Headers
             .ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray()));

        _logger.Information("Request Headers: {Method} {Path} {@Headers}",
            context.Request.Method,
            context.Request.Path,
            headers);

        await _next(context);
    }
}
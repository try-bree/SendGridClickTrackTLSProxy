using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SendGridClickTrackTLSProxy.HealthChecks;

public class SendGridHealthCheck : IHealthCheck
{
    private readonly SendGridHealthTracker _healthTracker;
    private readonly ILogger<SendGridHealthCheck> _logger;

    public SendGridHealthCheck(SendGridHealthTracker healthTracker, ILogger<SendGridHealthCheck> logger)
    {
        _healthTracker = healthTracker;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var (isHealthy, recentStatusCodes, message) = _healthTracker.GetHealthStatus();

            var data = new Dictionary<string, object>
            {
                ["recentStatusCodes"] = recentStatusCodes,
                ["requestCount"] = recentStatusCodes.Length,
                ["errorCount"] = recentStatusCodes.Count(code => code >= 400)
            };

            _logger.LogInformation("SendGrid health check: {Message}", message);

            return Task.FromResult(isHealthy
                ? HealthCheckResult.Healthy(message, data)
                : HealthCheckResult.Unhealthy(message, data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SendGrid health check");
            return Task.FromResult(HealthCheckResult.Unhealthy("Health check failed with exception", ex));
        }
    }
}
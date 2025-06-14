namespace SendGridClickTrackTLSProxy.HealthChecks;

using System.Collections.Concurrent;

public class SendGridHealthTracker
{
    private readonly ConcurrentQueue<int> _recentStatusCodes = new();
    private readonly object _lock = new();
    private readonly int _maxRequestCount;

    public SendGridHealthTracker(IConfiguration configuration)
    {
        _maxRequestCount = configuration.GetValue("SendGrid:HealthCheckMaxRequestCount", 10);
    }

    public void RecordResponse(int statusCode)
    {
        lock (_lock)
        {
            _recentStatusCodes.Enqueue(statusCode);
            // Keep only the last N responses based on configuration
            while (_recentStatusCodes.Count > _maxRequestCount)
            {
                _recentStatusCodes.TryDequeue(out _);
            }
        }
    }

    public (bool IsHealthy, int[] RecentStatusCodes, string Message) GetHealthStatus()
    {
        lock (_lock)
        {
            var statusCodes = _recentStatusCodes.ToArray();
            if (statusCodes.Length == 0)
            {
                return (true, statusCodes, "No recent requests to SendGrid");
            }

            var errorCodes = statusCodes.Where(code => code >= 400).ToArray();
            if (errorCodes.Length == 0)
            {
                return (true, statusCodes, $"All {statusCodes.Length} recent requests successful");
            }

            return (false, statusCodes,
                $"Found {errorCodes.Length} error responses out of {statusCodes.Length} recent requests: [{string.Join(", ", errorCodes)}]");
        }
    }
}
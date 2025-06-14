namespace SendGridClickTrackTLSProxy.Services;

public class SendGridHealthTrackingConfigService
{
    private readonly IConfiguration _configuration;
    private string[]? _trackingPathPrefixes;
    private string? _proxyHost;

    public SendGridHealthTrackingConfigService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string[] TrackingPathPrefixes
    {
        get
        {
            if (_trackingPathPrefixes == null)
            {
                var sendGridPaths = _configuration.GetSection("SendGrid:ClickTrackingPathsToMatch").Get<string[]>() ?? Array.Empty<string>();

                // Convert to path prefixes with leading slash
                _trackingPathPrefixes = sendGridPaths
                    .Select(path => $"/{path}")
                    .ToArray();
            }
            return _trackingPathPrefixes;
        }
    }

    public string ProxyHost
    {
        get
        {
            if (_proxyHost == null)
            {
                _proxyHost = _configuration["SendGrid:ClickTrackingCustomDomain"] ?? string.Empty; // cannot be null as we check config for null value and throw
            }
            return _proxyHost;
        }
    }
}
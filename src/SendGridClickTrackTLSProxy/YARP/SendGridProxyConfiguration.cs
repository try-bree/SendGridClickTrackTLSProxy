using Yarp.ReverseProxy.Configuration;

namespace SendGridClickTrackTLSProxy.YARP;

public class SendGridProxyConfiguration
{
    private readonly IConfiguration _configuration;
    const string SENDGRID_URI = "http://sendgrid.net/";

    public SendGridProxyConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;

        var customDomain = _configuration["SendGrid:ClickTrackingCustomDomain"];
        if (string.IsNullOrWhiteSpace(customDomain))
        {
            throw new ConfigurationException("SendGrid:ClickTrackingCustomDomain configuration is required but was not found or is null. The value must match the SendGrid configured click tracking custom domain. Only client requests matching this domain will be routed to SendGrid.");
        }
    }

    public IProxyConfigProvider CreateProxyConfig()
    {
        var sendGridPaths = _configuration.GetSection("SendGrid:ClickTrackingPathsToMatch").Get<string[]>() ?? Array.Empty<string>();
        var customDomain = _configuration["SendGrid:ClickTrackingCustomDomain"]!; // Safe to use ! now since we validated in constructor

        var routes = new List<RouteConfig>();
        foreach (var path in sendGridPaths)
        {
            var route = new RouteConfig
            {
                RouteId = $"sendgrid-{path}",
                ClusterId = "sendgrid-cluster",
                Match = new RouteMatch
                {
                    Path = $"/{path}/{{**catch-all}}",
                    Methods = new[] { "GET" },
                    Hosts = new[] { customDomain } // No need for null check anymore
                }
            };
            routes.Add(route);
        }

        var clusters = new[]
        {
            new ClusterConfig
            {
                ClusterId = "sendgrid-cluster",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["sendgrid"] = new DestinationConfig
                    {
                        Address = SENDGRID_URI
                    }
                }
            }
        };

        return new InMemoryConfigProvider(routes, clusters);
    }
}
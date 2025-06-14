using Yarp.ReverseProxy.Configuration;

namespace SendGridClickTrackTLSProxy.YARP;

public class InMemoryConfigProvider : IProxyConfigProvider
{
    private readonly InMemoryProxyConfig _config;

    public InMemoryConfigProvider(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        _config = new InMemoryProxyConfig(routes, clusters);
    }

    public IProxyConfig GetConfig() => _config;
}

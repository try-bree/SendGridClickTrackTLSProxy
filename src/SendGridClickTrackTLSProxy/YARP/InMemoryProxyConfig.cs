using Yarp.ReverseProxy.Configuration;
using Microsoft.Extensions.Primitives;

namespace SendGridClickTrackTLSProxy.YARP;

public class InMemoryProxyConfig : IProxyConfig
{
    public InMemoryProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        Routes = routes;
        Clusters = clusters;
        ChangeToken = new CancellationChangeToken(CancellationToken.None);
        RevisionId = Guid.NewGuid().ToString();
    }

    public IReadOnlyList<RouteConfig> Routes { get; }
    public IReadOnlyList<ClusterConfig> Clusters { get; }
    public IChangeToken ChangeToken { get; }
    public string RevisionId { get; }
}
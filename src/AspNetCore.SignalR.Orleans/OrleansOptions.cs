using Microsoft.AspNetCore.SignalR;
using Orleans;

namespace AspNetCore.SignalR.Orleans
{
    /// <summary>
    /// Options used to configure <see cref="OrleansHubLifetimeManager{THub}"/>.
    /// </summary>
    public class OrleansOptions<THub> where THub : Hub
    {
        public IClusterClient ClusterClient { get; set; }
    }
}

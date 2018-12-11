using Microsoft.AspNetCore.SignalR;
using Orleans;
using System;

namespace AspNetCore.SignalR.Orleans
{
    /// <summary>
    /// Options used to configure <see cref="OrleansHubLifetimeManager{THub}"/>.
    /// </summary>
    public class OrleansOptions<THub> where THub : Hub
    {
        public IClusterClient ClusterClient { get; set; }

        /// <summary>
        /// Gets or sets the time window server has to send a message before the cluster closes the connection.
        /// </summary>
        public TimeSpan? TimeoutInterval { get; set; } = null;
    }
}

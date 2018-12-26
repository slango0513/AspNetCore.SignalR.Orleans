using AspNetCore.SignalR.Orleans;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Messaging.SignalR;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OrleansDependencyInjectionExtensions
    {
        /// <summary>
        /// Adds scale-out to a <see cref="ISignalRServerBuilder"/>, using a shared Orleans cluster.
        /// </summary>
        /// <param name="signalrBuilder">The <see cref="ISignalRServerBuilder"/>.</param>
        /// <param name="clusterClient">The <see cref="IClusterClient"/>.</param>
        /// <returns>The same instance of the <see cref="ISignalRServerBuilder"/> for chaining.</returns>
        public static ISignalRServerBuilder AddOrleans<THub>(this ISignalRServerBuilder signalrBuilder, Action<OrleansOptions<THub>> configure) where THub : Hub
        {
            signalrBuilder.Services.Configure(configure);
            signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<THub>), typeof(OrleansHubLifetimeManager<THub>));
            signalrBuilder.Services.AddSingleton(typeof(IHubProxy<THub>), provider => new HubProxy<THub>(provider.GetRequiredService<IOptions<OrleansOptions<THub>>>().Value.ClusterClient));
            return signalrBuilder;
        }
    }
}

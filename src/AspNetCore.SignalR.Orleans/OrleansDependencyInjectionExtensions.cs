using AspNetCore.SignalR.Orleans;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans;
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
            signalrBuilder.Services.TryAddSingleton(typeof(IClientSetPartitioner<>), typeof(DefaultClientSetPartitioner<>));
            //signalrBuilder.Services.TryAddSingleton(typeof(IClientSetPartitioner<,>), typeof(DefaultClientSetPartitioner<,>));
            return signalrBuilder;
        }
    }
}

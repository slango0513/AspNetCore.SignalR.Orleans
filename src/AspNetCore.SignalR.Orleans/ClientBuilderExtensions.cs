using AspNetCore.SignalR.Orleans;
using AspNetCore.SignalR.Orleans.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Configuration;
using Orleans.Hosting;
using System;

namespace Orleans
{
    public static partial class ClientBuilderExtensions
    {
        public static IClientBuilder AddSignalR(this IClientBuilder builder, Action<SimpleMessageStreamProviderOptions> configureOptions)
        {
            builder.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, configureOptions)
                .ConfigureApplicationParts(manager =>
                {
                    manager.AddApplicationPart(typeof(IClientGrain).Assembly).WithReferences();
                });

            builder.ConfigureServices(services =>
            {
                //services.TryAddSingleton(typeof(IClientSetPartitioner<,>), typeof(DefaultClientSetPartitioner<,>));
                services.TryAddSingleton(typeof(IClientSetPartitioner<>), typeof(DefaultClientSetPartitioner<>));
            });

            return builder;
        }
    }
}

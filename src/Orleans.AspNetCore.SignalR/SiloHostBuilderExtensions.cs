using AspNetCore.SignalR.Orleans;
using AspNetCore.SignalR.Orleans.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Configuration;
using System;

namespace Orleans.Hosting
{
    public static partial class SiloHostBuilderExtensions
    {
        public static ISiloHostBuilder AddSignalR(this ISiloHostBuilder builder, Action<SimpleMessageStreamProviderOptions> configureOptions)
        {
            builder.AddSimpleMessageStreamProvider(Constants.STREAM_PROVIDER, configureOptions)
                //.Configure<GrainCollectionOptions>(options =>
                //{
                //    options.ClassSpecificCollectionAge[typeof(GroupGrain).FullName] = TimeSpan.FromMilliseconds(1000);
                //    options.ClassSpecificCollectionAge[typeof(UserGrain).FullName] = TimeSpan.FromMilliseconds(1000);
                //})
                .ConfigureApplicationParts(manager =>
                {
                    manager.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences();
                });

            builder.ConfigureServices(services =>
            {
                services.TryAddSingleton(typeof(IClientSetPartitioner<>), typeof(DefaultClientSetPartitioner<>));
            });

            try
            {
                builder = builder.AddMemoryGrainStorage("PubSubStore");
            }
            catch
            {
                // PubSub store was already added.
            }
            builder.AddMemoryGrainStorage(Constants.STORAGE_PROVIDER);
            //builder.UseTransactions();
            return builder;
        }
    }
}

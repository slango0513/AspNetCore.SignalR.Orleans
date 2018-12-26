using Orleans.Configuration;
using Orleans.Messaging.SignalR;
using Orleans.Messaging.SignalR.Internal;


namespace Orleans.Hosting
{
    public static partial class SignalRSiloHostBuilderExtensions
    {
        public static ISiloHostBuilder UseSignalR(this ISiloHostBuilder builder,
            bool fireAndForgetDelivery = SimpleMessageStreamProviderOptions.DEFAULT_VALUE_FIRE_AND_FORGET_DELIVERY)
        {
            builder.AddSimpleMessageStreamProvider(SignalRConstants.STREAM_PROVIDER, options =>
            {
                options.FireAndForgetDelivery = fireAndForgetDelivery;
            })
            .ConfigureApplicationParts(manager =>
            {
                manager.AddApplicationPart(typeof(ClientGrain).Assembly).WithReferences();
            })
            .AddMemoryGrainStorage("PubSubStore")
            .AddMemoryGrainStorage(InternalSignalRConstants.STORAGE_PROVIDER);

            return builder;
        }
    }
}

using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Messaging.SignalR;
using Orleans.Messaging.SignalR.Internal;

namespace Orleans
{
    public static partial class SignalRClientBuilderExtensions
    {
        public static IClientBuilder UseSignalR(this IClientBuilder builder,
            bool fireAndForgetDelivery = SimpleMessageStreamProviderOptions.DEFAULT_VALUE_FIRE_AND_FORGET_DELIVERY)
        {
            builder.AddSimpleMessageStreamProvider(SignalRConstants.STREAM_PROVIDER, options =>
            {
                options.FireAndForgetDelivery = fireAndForgetDelivery;
            })
            .ConfigureApplicationParts(manager =>
            {
                manager.AddApplicationPart(typeof(IClientGrain).Assembly).WithReferences();
            });

            return builder;
        }
    }
}

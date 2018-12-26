using Orleans.Messaging.SignalR;
using Orleans.Streams;
using System;

namespace Orleans
{
    public static partial class HubProxyGrainFactoryExtensions
    {
        public static IHubProxy GetHubProxy(this IGrainFactory grainFactory, IStreamProvider streamProvider, Guid hubTypeId)
        {
            return new HubProxy(grainFactory, streamProvider, hubTypeId);
        }
    }
}

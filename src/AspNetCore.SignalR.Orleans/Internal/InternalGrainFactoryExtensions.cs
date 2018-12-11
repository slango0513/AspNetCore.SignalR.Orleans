using AspNetCore.SignalR.Orleans.Internal;
using System;

namespace Orleans
{
    internal static partial class InternalGrainFactoryExtensions
    {
        public static IHubLifetimeManagerGrain GetHubLifetimeManagerGrain(this IGrainFactory grainFactory, Guid hubLifetimeManagerId, Guid hubTypeId)
        {
            return grainFactory.GetGrain<IHubLifetimeManagerGrain>(hubLifetimeManagerId, hubTypeId.ToString());
        }
    }
}

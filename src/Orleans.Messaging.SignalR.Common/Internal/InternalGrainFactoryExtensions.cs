using Orleans.Messaging.SignalR.Internal;
using System;

namespace Orleans
{
    internal static partial class InternalGrainFactoryExtensions
    {
        public static IClientGrain GetClientGrain(this IGrainFactory grainFactory, string connectionId, Guid hubTypeId)
        {
            return grainFactory.GetGrain<IClientGrain>(HubTypedKeyUtils.ToHubTypedKeyString(connectionId, hubTypeId));
        }

        public static IGroupGrain GetGroupGrain(this IGrainFactory grainFactory, string groupName, Guid hubTypeId)
        {
            return grainFactory.GetGrain<IGroupGrain>(HubTypedKeyUtils.ToHubTypedKeyString(groupName, hubTypeId));
        }

        public static IUserGrain GetUserGrain(this IGrainFactory grainFactory, string userId, Guid hubTypeId)
        {
            return grainFactory.GetGrain<IUserGrain>(HubTypedKeyUtils.ToHubTypedKeyString(userId, hubTypeId));
        }
    }
}

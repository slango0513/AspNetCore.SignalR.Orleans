using AspNetCore.SignalR.Orleans;
using AspNetCore.SignalR.Orleans.Internal;
using System;

namespace Orleans
{
    public static partial class GrainFactoryExtensions
    {
        public static IClientAddressable GetClient(this IGrainFactory grainFactory, string connectionId, Guid hubTypeId)
        {
            return grainFactory.GetGrain<IClientGrain>(HubTypedKeyUtils.ToHubTypedKeyString(connectionId, hubTypeId)).AsReference<IClientAddressable>();
        }

        public static IGroupAddressable GetGroup(this IGrainFactory grainFactory, string groupName, Guid hubTypeId)
        {
            return grainFactory.GetGrain<IGroupGrain>(HubTypedKeyUtils.ToHubTypedKeyString(groupName, hubTypeId)).AsReference<IGroupAddressable>();
        }

        public static IUserAddressable GetUser(this IGrainFactory grainFactory, string userId, Guid hubTypeId)
        {
            return grainFactory.GetGrain<IUserGrain>(HubTypedKeyUtils.ToHubTypedKeyString(userId, hubTypeId)).AsReference<IUserAddressable>();
        }
    }
}

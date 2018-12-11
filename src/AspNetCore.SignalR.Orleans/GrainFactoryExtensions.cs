using AspNetCore.SignalR.Orleans;
using Microsoft.AspNetCore.SignalR;

namespace Orleans
{
    public static partial class GrainFactoryExtensions
    {
        public static IClientAddressable GetClient<THub>(this IGrainFactory grainFactory, string connectionId) where THub : Hub
        {
            return grainFactory.GetClient(connectionId, typeof(THub).GUID);
        }

        public static IGroupAddressable GetGroup<THub>(this IGrainFactory grainFactory, string groupName) where THub : Hub
        {
            return grainFactory.GetGroup(groupName, typeof(THub).GUID);
        }

        public static IUserAddressable GetUser<THub>(this IGrainFactory grainFactory, string userId) where THub : Hub
        {
            return grainFactory.GetUser(userId, typeof(THub).GUID);
        }
    }
}

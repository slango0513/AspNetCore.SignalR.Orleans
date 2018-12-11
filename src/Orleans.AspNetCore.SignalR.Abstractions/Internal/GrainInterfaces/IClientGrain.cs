using System;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    public interface IClientGrain : IGrainWithHubTypedStringKey, IClientAddressable
    {
        /// <summary>
        /// Called by <see cref="IHubLifetimeManagerGrain"/>.
        /// </summary>
        Task OnConnectedAsync(Guid hubLifetimeManagerId);

        /// <summary>
        /// Called by <see cref="IHubLifetimeManagerGrain"/>.
        /// </summary>
        Task OnDisconnectedAsync();

        /// <summary>
        /// Called by <see cref="IGroupPartitionGrain"/>.
        /// </summary>
        Task OnAddToGroupAsync(string groupName);

        /// <summary>
        /// Called by <see cref="IGroupPartitionGrain"/>.
        /// </summary>
        Task OnRemoveFromGroupAsync(string groupName);

        /// <summary>
        /// Called by <see cref="IUserPartitionGrain"/>.
        /// </summary>
        Task OnAddToUserAsync(string userId);

        /// <summary>
        /// Called by <see cref="IUserPartitionGrain"/>.
        /// </summary>
        Task OnRemoveFromUserAsync(string userId);
    }
}

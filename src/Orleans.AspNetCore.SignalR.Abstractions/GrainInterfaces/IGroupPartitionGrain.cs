using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public interface IGroupPartitionGrain : IClientSetPartitionGrain
    {
        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task AddToGroupAsync(string connectionId, string groupName);

        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task RemoveFromGroupAsync(string connectionId, string groupName);
    }
}

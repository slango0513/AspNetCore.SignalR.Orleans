using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public interface IUserPartitionGrain : IClientSetPartitionGrain
    {
        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task AddToUserAsync(string connectionId, string userId);

        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task RemoveFromUserAsync(string connectionId, string userId);
    }
}

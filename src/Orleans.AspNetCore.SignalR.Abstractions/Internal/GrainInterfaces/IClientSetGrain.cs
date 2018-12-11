using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    public interface IClientSetGrain : IGrainWithHubTypedStringKey, IClientSetAddressable
    {
        /// <summary>
        /// Called by <see cref="IClientSetPartitionGrain"/>.
        /// </summary>
        Task<int> AddToClientSetAsync(string connectionId);

        /// <summary>
        /// Called by <see cref="IClientSetPartitionGrain"/>.
        /// </summary>
        Task<int> RemoveFromClientSetAsync(string connectionId);
    }
}

using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal class UserPartitionGrainState : ClientSetPartitionGrainState
    {
    }

    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class UserPartitionGrain : ClientSetPartitionGrain<UserPartitionGrainState>, IUserPartitionGrain
    {
        private readonly ILogger _logger;

        public UserPartitionGrain(ILogger<UserPartitionGrain> logger)
        {
            _logger = logger;
        }

        public async Task AddToUserAsync(string connectionId, string userId)
        {
            var hubTypeId = this.GetHubTypeId();

            await GrainFactory.GetClientGrain(connectionId, hubTypeId).OnAddToUserAsync(userId);

            await AddToClientSetAsync(connectionId, GrainFactory.GetUserGrain(userId, hubTypeId));
        }

        public async Task RemoveFromUserAsync(string connectionId, string userId)
        {
            var hubTypeId = this.GetHubTypeId();

            await GrainFactory.GetClientGrain(connectionId, hubTypeId).OnRemoveFromUserAsync(userId);

            await RemoveFromClientSetAsync(connectionId, GrainFactory.GetUserGrain(userId, hubTypeId));
        }
    }
}

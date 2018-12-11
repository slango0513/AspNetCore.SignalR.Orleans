using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal class GroupPartitionGrainState : ClientSetPartitionGrainState
    {
    }

    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class GroupPartitionGrain : ClientSetPartitionGrain<GroupPartitionGrainState>, IGroupPartitionGrain
    {
        private readonly ILogger _logger;

        public GroupPartitionGrain(ILogger<GroupPartitionGrain> logger)
        {
            _logger = logger;
        }

        public async Task AddToGroupAsync(string connectionId, string groupName)
        {
            var hubTypeId = this.GetHubTypeId();

            await GrainFactory.GetClientGrain(connectionId, hubTypeId).OnAddToGroupAsync(groupName);

            await AddToClientSetAsync(connectionId, GrainFactory.GetGroupGrain(groupName, hubTypeId));
        }

        public async Task RemoveFromGroupAsync(string connectionId, string groupName)
        {
            var hubTypeId = this.GetHubTypeId();

            await GrainFactory.GetClientGrain(connectionId, hubTypeId).OnRemoveFromGroupAsync(groupName);

            await RemoveFromClientSetAsync(connectionId, GrainFactory.GetGroupGrain(groupName, hubTypeId));
        }
    }
}

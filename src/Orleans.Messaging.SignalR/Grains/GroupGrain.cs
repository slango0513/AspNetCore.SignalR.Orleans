using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Orleans.Messaging.SignalR.Internal
{
    internal class GroupGrainState : ClientsGrainState
    {
    }

    [StorageProvider(ProviderName = InternalSignalRConstants.STORAGE_PROVIDER)]
    internal class GroupGrain : ClientsGrain<GroupGrainState>, IGroupGrain
    {
        public GroupGrain(ILogger<GroupGrain> logger) : base(logger)
        {
        }
    }
}

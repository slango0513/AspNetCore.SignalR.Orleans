using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal class GroupGrainState : ClientSetGrainState
    {
    }

    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class GroupGrain : ClientSetGrain<GroupGrainState>, IGroupGrain
    {
        private readonly ILogger _logger;

        public GroupGrain(ILogger<GroupGrain> logger)
        {
            _logger = logger;
        }
    }
}

using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Orleans.Messaging.SignalR.Internal
{
    internal class UserGrainState : ClientsGrainState
    {
    }

    [StorageProvider(ProviderName = InternalSignalRConstants.STORAGE_PROVIDER)]
    internal class UserGrain : ClientsGrain<UserGrainState>, IUserGrain
    {
        public UserGrain(ILogger<UserGrain> logger) : base(logger)
        {
        }
    }
}

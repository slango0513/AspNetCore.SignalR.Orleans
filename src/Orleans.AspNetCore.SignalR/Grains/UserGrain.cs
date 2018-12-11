using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal class UserGrainState : ClientSetGrainState
    {
    }

    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class UserGrain : ClientSetGrain<UserGrainState>, IUserGrain
    {
        private readonly ILogger _logger;

        public UserGrain(ILogger<UserGrain> logger)
        {
            _logger = logger;
        }
    }
}

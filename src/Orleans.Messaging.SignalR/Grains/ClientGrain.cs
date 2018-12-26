using Microsoft.Extensions.Logging;
using Orleans.Providers;
using System;
using System.Threading.Tasks;

namespace Orleans.Messaging.SignalR.Internal
{
    internal class ClientGrainState
    {
        public bool Connected { get; set; }

        public Guid HubLifetimeManagerId { get; set; }
    }

    [StorageProvider(ProviderName = InternalSignalRConstants.STORAGE_PROVIDER)]
    internal class ClientGrain : Grain<ClientGrainState>, IClientGrain
    {
        private readonly ILogger _logger;

        public ClientGrain(ILogger<ClientGrain> logger)
        {
            _logger = logger;
        }

        public async Task OnConnectedAsync(Guid hubLifetimeManagerId)
        {
            State.HubLifetimeManagerId = hubLifetimeManagerId;

            State.Connected = true;
            await WriteStateAsync();
        }

        public async Task OnDisconnectedAsync()
        {
            var connectionId = this.GetId();

            await GetStreamProvider(SignalRConstants.STREAM_PROVIDER)
                .GetStream<EventArgs>(InternalSignalRConstants.DISCONNECTION_STREAM_ID, connectionId)
                .OnNextAsync(EventArgs.Empty);

            State.Connected = false;
            await WriteStateAsync();

            DeactivateOnIdle();
        }

        public Task SendConnectionAsync(string methodName, object[] args)
        {
            var connectionId = this.GetId();
            return GetStreamProvider(SignalRConstants.STREAM_PROVIDER)
                .GetStream<SendClientInvocationMessage>(State.HubLifetimeManagerId, InternalSignalRConstants.SEND_CLIENT_MESSAGE_STREAM_NAMESPACE)
                .OnNextAsync(new SendClientInvocationMessage(methodName, args, connectionId));
        }

        public Task DeactivateAsync()
        {
            DeactivateOnIdle();
            return Task.CompletedTask;
        }
    }
}

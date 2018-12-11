using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal class ClientGrainState
    {
        public bool Aborted { get; set; } = false;

        public Guid HubLifetimeManagerId { get; set; } = default;

        public ISet<string> GroupNames { get; set; } = new HashSet<string>();

        public string UserId { get; set; } = default;
    }

    [StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class ClientGrain : Grain<ClientGrainState>, IClientGrain
    {
        private readonly IClientSetPartitioner<IGroupPartitionGrain> _groupPartitioner;
        private readonly IClientSetPartitioner<IUserPartitionGrain> _userPartitioner;
        private readonly ILogger _logger;

        public ClientGrain(IClientSetPartitioner<IGroupPartitionGrain> groupPartitioner,
            IClientSetPartitioner<IUserPartitionGrain> userPartitioner,
            ILogger<ClientGrain> logger)
        {
            _groupPartitioner = groupPartitioner;
            _userPartitioner = userPartitioner;
            _logger = logger;
        }

        private string _connectionId;
        private Guid _hubTypeId;
        private IAsyncObserver<ClientInvocationMessage> _hubLifetimeManagerObserver;

        public override Task OnActivateAsync()
        {
            _connectionId = this.GetId();
            _hubTypeId = this.GetHubTypeId();

            if (!State.Aborted)
            {
                return ActivateStreamAsync();
            }
            return Task.CompletedTask;
        }

        private Task ActivateStreamAsync()
        {
            var provider = GetStreamProvider(Constants.STREAM_PROVIDER);
            _hubLifetimeManagerObserver = provider.GetStream<ClientInvocationMessage>(State.HubLifetimeManagerId, Constants.CLIENT_MESSAGE_STREAM_NAMESPACE);
            return Task.CompletedTask;
        }

        public async Task OnConnectedAsync(Guid hubLifetimeManagerId)
        {
            State.HubLifetimeManagerId = hubLifetimeManagerId;
            await ActivateStreamAsync();
            State.Aborted = false;
            await WriteStateAsync();
        }

        public async Task OnDisconnectedAsync()
        {
            var groupTasks = State.GroupNames.Select(name => _groupPartitioner.GetPartitionGrain(GrainFactory, name, _hubTypeId).RemoveFromGroupAsync(_connectionId, name));
            await Task.WhenAll(groupTasks);

            if (!string.IsNullOrEmpty(State.UserId))
            {
                await _userPartitioner.GetPartitionGrain(GrainFactory, State.UserId, _hubTypeId).RemoveFromUserAsync(_connectionId, State.UserId);
            }

            State.Aborted = true;
            await WriteStateAsync();
            DeactivateOnIdle();
        }

        public async Task OnAddToGroupAsync(string groupName)
        {
            if (State.GroupNames.Add(groupName))
            {
                await WriteStateAsync();
            }
        }

        public async Task OnRemoveFromGroupAsync(string groupName)
        {
            if (State.GroupNames.Remove(groupName))
            {
                await WriteStateAsync();
            }
        }

        public async Task OnAddToUserAsync(string userId)
        {
            State.UserId = userId;
            await WriteStateAsync();
        }

        public async Task OnRemoveFromUserAsync(string userId)
        {
            State.UserId = null;
            await WriteStateAsync();
        }

        public Task<ISet<string>> GetGroupNamesAsync()
        {
            return Task.FromResult(State.GroupNames);
        }

        public Task<string> GetUserIdAsync()
        {
            return Task.FromResult(State.UserId);
        }

        public async Task SendConnectionAsync(string methodName, object[] args)
        {
            if (State.Aborted)
            {
                throw new InvalidOperationException(nameof(State.Aborted));
            }

            await _hubLifetimeManagerObserver.OnNextAsync(new ClientInvocationMessage(methodName, args, _connectionId));
        }
    }
}

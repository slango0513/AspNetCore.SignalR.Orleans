using AspNetCore.SignalR.Orleans.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable where THub : Hub
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly Guid _hubTypeId = typeof(THub).GUID;

        private readonly OrleansOptions<THub> _options;
        private readonly IClusterClient _clusterClient;
        private readonly IClientSetPartitioner<IGroupPartitionGrain> _groupPartitioner;
        private readonly IClientSetPartitioner<IUserPartitionGrain> _userPartitioner;
        private readonly IUserIdProvider _userIdProvider;
        private readonly ILogger _logger;

        public OrleansHubLifetimeManager(IOptions<OrleansOptions<THub>> options,
            IClientSetPartitioner<IGroupPartitionGrain> groupPartitioner,
            IClientSetPartitioner<IUserPartitionGrain> userPartitioner,
            IUserIdProvider userIdProvider,
            ILogger<OrleansHubLifetimeManager<THub>> logger)
        {
            _options = options.Value;
            _clusterClient = options.Value.ClusterClient;
            _groupPartitioner = groupPartitioner;
            _userPartitioner = userPartitioner;
            _userIdProvider = userIdProvider;
            _logger = logger;
        }

        private bool isInitialized = false;
        private IAsyncStream<ClientInvocationMessage> _clientMessageStream = default;
        private IAsyncStream<AllInvocationMessage> _allMessageStream = default;
        private ConcurrentDictionary<string, HubConnectionContext> connectionsById = new ConcurrentDictionary<string, HubConnectionContext>();
        private IDisposable heartbeatDisposable = default;

        private async Task ConnectToClusterAsync()
        {
            await EnsureOrleansClusterConnection();

            OrleansLog.Subscribing(_logger, _id);

            var provider = _clusterClient.GetStreamProvider(Constants.STREAM_PROVIDER);

            _clientMessageStream = provider.GetStream<ClientInvocationMessage>(_id, Constants.CLIENT_MESSAGE_STREAM_NAMESPACE);
            await _clientMessageStream.SubscribeAsync(async (message, token) => await OnReceivedClientMessageAsync(message));

            _allMessageStream = provider.GetStream<AllInvocationMessage>(_hubTypeId, Constants.HUB_MESSAGE_STREAM_NAMESPACE);
            await _allMessageStream.SubscribeAsync(async (message, token) => await OnReceivedAllMessageAsync(message));

            try
            {
                await _clusterClient.GetHubLifetimeManagerGrain(_id, _hubTypeId).OnInitializeAsync(_options.TimeoutInterval ?? TimeSpan.FromSeconds(30));
                // Tick heartbeat
                heartbeatDisposable = Observable.Interval(TimeSpan.FromSeconds(1))
                    .Subscribe(async value =>
                    {
                        await _clusterClient.GetHubLifetimeManagerGrain(_id, _hubTypeId).OnHeartbeatAsync();
                    });
            }
            catch (Exception e)
            {
                OrleansLog.InternalMessageFailed(_logger, e);
            }
        }

        private int _retries = 0;

        private async Task EnsureOrleansClusterConnection()
        {
            if (_clusterClient.IsInitialized)
            {
                OrleansLog.Connected(_logger);
                return;
            }

            OrleansLog.NotConnected(_logger);
            await _clusterClient.Connect(async e =>
            {
                if (_retries >= 8)
                {
                    throw e;
                }

                OrleansLog.ConnectionFailed(_logger, e);
                await Task.Delay(2000);
                _retries++;
                return true;
            });
            OrleansLog.ConnectionRestored(_logger);
        }

        private async Task OnReceivedClientMessageAsync(ClientInvocationMessage message)
        {
            OrleansLog.ReceivedFromStream(_logger, _id);
            if (connectionsById.TryGetValue(message.ConnectionId, out var connection))
            {
                var invocationMessage = new InvocationMessage(message.MethodName, message.Args);
                try
                {
                    await connection.WriteAsync(invocationMessage).AsTask();
                }
                catch (Exception e)
                {
                    OrleansLog.FailedWritingMessage(_logger, e);
                }
            }
        }

        private async Task OnReceivedAllMessageAsync(AllInvocationMessage message)
        {
            OrleansLog.ReceivedFromStream(_logger, _id);
            var invocationMessage = new InvocationMessage(message.MethodName, message.Args);

            var tasks = connectionsById.Where(pair => !pair.Value.ConnectionAborted.IsCancellationRequested && !message.ExcludedConnectionIds.Contains(pair.Key))
                .Select(pair => pair.Value.WriteAsync(invocationMessage).AsTask());

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                OrleansLog.FailedWritingMessage(_logger, e);
            }
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            if (!isInitialized)
            {
                await ConnectToClusterAsync();
                isInitialized = true;
            }

            var connectionId = connection.ConnectionId;
            await _clusterClient.GetHubLifetimeManagerGrain(_id, _hubTypeId).OnConnectedAsync(connectionId);

            var userId = _userIdProvider.GetUserId(connection);
            if (!string.IsNullOrEmpty(userId))
            {
                await _userPartitioner.GetPartitionGrain(_clusterClient, userId, _hubTypeId).AddToUserAsync(connectionId, userId);
            }
            connectionsById.TryAdd(connectionId, connection);
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            var connectionId = connection.ConnectionId;
            await _clusterClient.GetHubLifetimeManagerGrain(_id, _hubTypeId).OnDisconnectedAsync(connectionId);

            var userId = _userIdProvider.GetUserId(connection);
            if (!string.IsNullOrEmpty(userId))
            {
                await _userPartitioner.GetPartitionGrain(_clusterClient, userId, _hubTypeId).RemoveFromUserAsync(connectionId, userId);
            }

            connectionsById.TryRemove(connectionId, out _);
        }

        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return _groupPartitioner.GetPartitionGrain(_clusterClient, groupName, _hubTypeId).AddToGroupAsync(connectionId, groupName);
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return _groupPartitioner.GetPartitionGrain(_clusterClient, groupName, _hubTypeId).RemoveFromGroupAsync(connectionId, groupName);
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendAllExceptAsync(methodName, args, Enumerable.Empty<string>().ToList(), cancellationToken);
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            return _allMessageStream.OnNextAsync(new AllInvocationMessage(methodName, args, excludedConnectionIds));
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return _clusterClient.GetClient(connectionId, _hubTypeId).SendConnectionAsync(methodName, args);
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            var tasks = connectionIds.Select(id => SendConnectionAsync(id, methodName, args, cancellationToken));
            return Task.WhenAll(tasks);
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return _clusterClient.GetGroup(groupName, _hubTypeId).SendClientSetAsync(methodName, args);
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            return _clusterClient.GetGroup(groupName, _hubTypeId).SendClientSetExceptAsync(methodName, args, excludedConnectionIds);
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            var tasks = groupNames.Select(name => SendGroupAsync(name, methodName, args, cancellationToken));
            return Task.WhenAll(tasks);
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return _clusterClient.GetUser(userId, _hubTypeId).SendClientSetAsync(methodName, args);
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            var tasks = userIds.Select(id => SendUserAsync(id, methodName, args, cancellationToken));
            return Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            if (!isInitialized)
            {
                return;
            }

            heartbeatDisposable.Dispose();

            OrleansLog.Unsubscribe(_logger, _id);

            var clientHandles = _clientMessageStream.GetAllSubscriptionHandles().Result;
            var clientTasks = clientHandles.Select(handle => handle.UnsubscribeAsync()).ToArray();
            Task.WaitAll(clientTasks);

            //var allHandles = _allMessageStream.GetAllSubscriptionHandles().Result;
            //var allTasks = clientHandles.Select(handle => handle.UnsubscribeAsync()).ToArray();
            //Task.WaitAll(allTasks);

            var task = _clusterClient.GetHubLifetimeManagerGrain(_id, _hubTypeId).AbortAsync();
            Task.WaitAll(task);
        }
    }
}

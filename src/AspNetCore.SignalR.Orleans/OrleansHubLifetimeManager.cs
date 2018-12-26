using AspNetCore.SignalR.Orleans.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Messaging.SignalR;
using Orleans.Messaging.SignalR.Internal;
using Orleans.Streams;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public class OrleansHubLifetimeManager<THub> : HubLifetimeManager<THub>, IDisposable where THub : Hub
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly Guid _hubTypeId = typeof(THub).GUID;

        private readonly IClusterClient _clusterClient;
        private readonly ILogger _logger;

        public OrleansHubLifetimeManager(IOptions<OrleansOptions<THub>> options, IHubProxy<THub> hubProxy, ILogger<OrleansHubLifetimeManager<THub>> logger)
        {
            _clusterClient = options.Value.ClusterClient;
            HubProxy = hubProxy;
            _logger = logger;
        }

        public IHubProxy HubProxy { get; }

        private bool _initialized;
        private IAsyncStream<SendClientInvocationMessage> _clientMessageStream;
        private StreamSubscriptionHandle<SendClientInvocationMessage> _clientMessageHandle;
        private StreamSubscriptionHandle<SendAllInvocationMessage> _allMessageHandle;
        private ConcurrentDictionary<string, HubConnectionContext> connectionsById = new ConcurrentDictionary<string, HubConnectionContext>();

        private async Task InitializeAsync()
        {
            await EnsureOrleansClusterConnection();

            OrleansLog.Subscribing(_logger, _id);

            try
            {
                _clientMessageStream = _clusterClient.GetStreamProvider(SignalRConstants.STREAM_PROVIDER).GetStream<SendClientInvocationMessage>(_id, InternalSignalRConstants.SEND_CLIENT_MESSAGE_STREAM_NAMESPACE);
                _clientMessageHandle = await _clientMessageStream.SubscribeAsync(async (message, token) => await OnReceivedAsync(message));
                _allMessageHandle = await HubProxy.AllMessageStream.SubscribeAsync(async (message, token) => await OnReceivedAsync(message));
            }
            catch (Exception e)
            {
                OrleansLog.InternalMessageFailed(_logger, e);
            }
        }

        private int _retries = 0;

        private async Task EnsureOrleansClusterConnection()
        {
            if (_clusterClient == null)
            {
                throw new NullReferenceException(nameof(_clusterClient));
            }

            if (_clusterClient.IsInitialized)
            {
                OrleansLog.Connected(_logger);
                return;
            }

            OrleansLog.NotConnected(_logger);
            await _clusterClient.Connect(async ex =>
            {
                if (_retries >= 5)
                {
                    throw ex;
                }

                OrleansLog.ConnectionFailed(_logger, ex);
                await Task.Delay(2000);
                _retries++;
                return true;
            });
            OrleansLog.ConnectionRestored(_logger);
        }

        private async Task OnReceivedAsync(SendClientInvocationMessage message)
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

        private async Task OnReceivedAsync(SendAllInvocationMessage message)
        {
            OrleansLog.ReceivedFromStream(_logger, _id);
            var invocationMessage = new InvocationMessage(message.MethodName, message.Args);

            var tasks = connectionsById
                .Where(pair => !pair.Value.ConnectionAborted.IsCancellationRequested && !message.ExcludedConnectionIds.Contains(pair.Key))
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
            if (!_initialized)
            {
                _initialized = true;
                await InitializeAsync();
            }

            var connectionId = connection.ConnectionId;
            await HubProxy.OnConnectedAsync(_id, connectionId, connection.UserIdentifier);
            connectionsById.TryAdd(connectionId, connection);
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            var connectionId = connection.ConnectionId;
            await HubProxy.OnDisconnectedAsync(connectionId, connection.UserIdentifier);
            connectionsById.TryRemove(connectionId, out _);
        }

        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return HubProxy.AddToGroupAsync(connectionId, groupName);
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            return HubProxy.RemoveFromGroupAsync(connectionId, groupName);
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendAllAsync(methodName, args);
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendAllExceptAsync(methodName, args, excludedConnectionIds);
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendClientAsync(connectionId, methodName, args);
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendClientsAsync(connectionIds, methodName, args);
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendGroupAsync(groupName, methodName, args);
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendGroupExceptAsync(groupName, methodName, args, excludedConnectionIds);
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendGroupsAsync(groupNames, methodName, args);
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendUserAsync(userId, methodName, args);
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return HubProxy.SendUsersAsync(userIds, methodName, args);
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            OrleansLog.Unsubscribe(_logger, _id);

            var tasks = connectionsById.Keys.Select(id => _clusterClient.GetStreamProvider(SignalRConstants.STREAM_PROVIDER)
                .GetStream<EventArgs>(InternalSignalRConstants.DISCONNECTION_STREAM_ID, id)
                .OnNextAsync(EventArgs.Empty));
            Task.WaitAll(tasks.ToArray());

            Task.WaitAll(_allMessageHandle.UnsubscribeAsync());
        }
    }
}

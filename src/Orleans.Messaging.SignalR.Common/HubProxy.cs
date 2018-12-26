using Orleans.Messaging.SignalR.Internal;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Messaging.SignalR
{
    public class HubProxy : IHubProxy
    {
        private readonly IGrainFactory _grainFactory;
        private readonly IStreamProvider _streamProvider;
        private readonly Guid _hubTypeId;

        public HubProxy(IGrainFactory grainFactory, IStreamProvider streamProvider, Guid hubTypeId)
        {
            _grainFactory = grainFactory ?? throw new ArgumentNullException(nameof(grainFactory));
            _streamProvider = streamProvider ?? throw new ArgumentNullException(nameof(streamProvider));
            _hubTypeId = hubTypeId;

            AllMessageStream = _streamProvider.GetStream<SendAllInvocationMessage>(_hubTypeId, InternalSignalRConstants.SEND_All_MESSAGE_STREAM_NAMESPACE);
        }

        public IAsyncStream<SendAllInvocationMessage> AllMessageStream { get; }

        #region HubLifetimeManager Support
        public async Task OnConnectedAsync(Guid managerId, string connectionId, string userId = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (userId != null)
            {
                await _grainFactory.GetUserGrain(userId, _hubTypeId).AddToClientsAsync(connectionId);
            }
            await _grainFactory.GetClientGrain(connectionId, _hubTypeId).OnConnectedAsync(managerId);
        }

        public async Task OnDisconnectedAsync(string connectionId, string userId = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (userId != null)
            {
                await _grainFactory.GetUserGrain(userId, _hubTypeId).RemoveFromClientsAsync(connectionId);
            }
            await _grainFactory.GetClientGrain(connectionId, _hubTypeId).OnDisconnectedAsync();
        }
        #endregion

        #region GroupManager Support
        public Task AddToGroupAsync(string connectionId, string groupName)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            return _grainFactory.GetGroupGrain(groupName, _hubTypeId).AddToClientsAsync(connectionId);
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            return _grainFactory.GetGroupGrain(groupName, _hubTypeId).RemoveFromClientsAsync(connectionId);
        }
        #endregion

        #region HubClients Support
        public Task SendAllAsync(string method, object[] args)
        {
            return SendAllExceptAsync(method, args, Enumerable.Empty<string>().ToList());
        }

        public Task SendAllExceptAsync(string method, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            if (excludedConnectionIds == null)
            {
                throw new ArgumentNullException(nameof(excludedConnectionIds));
            }

            return AllMessageStream.OnNextAsync(new SendAllInvocationMessage(method, args, excludedConnectionIds));
        }

        public Task SendClientAsync(string connectionId, string method, object[] args)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            return _grainFactory.GetClientGrain(connectionId, _hubTypeId).SendConnectionAsync(method, args);
        }

        public Task SendClientsAsync(IReadOnlyList<string> connectionIds, string method, object[] args)
        {
            if (connectionIds == null)
            {
                throw new ArgumentNullException(nameof(connectionIds));
            }

            return SendClientsExceptAsync(connectionIds, method, args, Enumerable.Empty<string>().ToList());
        }

        public Task SendClientsExceptAsync(IReadOnlyList<string> connectionIds, string method, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            if (connectionIds == null)
            {
                throw new ArgumentNullException(nameof(connectionIds));
            }
            if (excludedConnectionIds == null)
            {
                throw new ArgumentNullException(nameof(excludedConnectionIds));
            }
            var tasks = connectionIds.Where(id => !excludedConnectionIds.Contains(id)).Select(id => _grainFactory.GetClientGrain(id, _hubTypeId).SendConnectionAsync(method, args));
            return Task.WhenAll(tasks);
        }

        public Task SendGroupAsync(string groupName, string method, object[] args)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            return _grainFactory.GetGroupGrain(groupName, _hubTypeId).SendConnectionsAsync(method, args);
        }

        public Task SendGroupExceptAsync(string groupName, string method, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }
            if (excludedConnectionIds == null)
            {
                throw new ArgumentNullException(nameof(excludedConnectionIds));
            }

            return _grainFactory.GetGroupGrain(groupName, _hubTypeId).SendConnectionsExceptAsync(method, args, excludedConnectionIds);
        }

        public Task SendGroupsAsync(IReadOnlyList<string> groupNames, string method, object[] args)
        {
            if (groupNames == null)
            {
                throw new ArgumentNullException(nameof(groupNames));
            }

            var tasks = groupNames.Select(name => _grainFactory.GetGroupGrain(name, _hubTypeId).SendConnectionsAsync(method, args));
            return Task.WhenAll(tasks);
        }

        public Task SendUserAsync(string userId, string method, object[] args)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            return _grainFactory.GetUserGrain(userId, _hubTypeId).SendConnectionsAsync(method, args);
        }

        public Task SendUsersAsync(IReadOnlyList<string> userIds, string method, object[] args)
        {
            if (userIds == null)
            {
                throw new ArgumentNullException(nameof(userIds));
            }

            var tasks = userIds.Select(id => _grainFactory.GetUserGrain(id, _hubTypeId).SendConnectionsAsync(method, args));
            return Task.WhenAll(tasks);
        }

        public Task SendUsersExceptAsync(IReadOnlyList<string> userIds, string method, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            if (userIds == null)
            {
                throw new ArgumentNullException(nameof(userIds));
            }
            if (excludedConnectionIds == null)
            {
                throw new ArgumentNullException(nameof(excludedConnectionIds));
            }

            var tasks = userIds.Select(id => _grainFactory.GetUserGrain(id, _hubTypeId).SendConnectionsExceptAsync(method, args, excludedConnectionIds));
            return Task.WhenAll(tasks);
        }
        #endregion

        #region Deactive Support
        public Task DeactiveClientAsync(string connectionId)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            return _grainFactory.GetClientGrain(connectionId, _hubTypeId).DeactivateAsync();
        }

        public Task DeactiveGroupAsync(string groupName)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            return _grainFactory.GetGroupGrain(groupName, _hubTypeId).DeactivateAsync();
        }

        public Task DeactiveUserAsync(string userId)
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            return _grainFactory.GetUserGrain(userId, _hubTypeId).DeactivateAsync();
        }
        #endregion
    }
}

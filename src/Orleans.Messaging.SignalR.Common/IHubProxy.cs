using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Messaging.SignalR
{
    public interface IHubProxy
    {
        IAsyncStream<SendAllInvocationMessage> AllMessageStream { get; }

        Task OnConnectedAsync(Guid managerId, string connectionId, string userId = default);

        Task OnDisconnectedAsync(string connectionId, string userId = default);

        Task AddToGroupAsync(string connectionId, string groupName);

        Task RemoveFromGroupAsync(string connectionId, string groupName);

        Task SendAllAsync(string method, object[] args);

        Task SendAllExceptAsync(string method, object[] args, IReadOnlyList<string> excludedConnectionIds);

        Task SendClientAsync(string connectionId, string method, object[] args);

        Task SendClientsAsync(IReadOnlyList<string> connectionIds, string method, object[] args);

        Task SendClientsExceptAsync(IReadOnlyList<string> connectionIds, string method, object[] args, IReadOnlyList<string> excludedConnectionIds);

        Task SendGroupAsync(string groupName, string method, object[] args);

        Task SendGroupExceptAsync(string groupName, string method, object[] args, IReadOnlyList<string> excludedConnectionIds);

        Task SendGroupsAsync(IReadOnlyList<string> groupNames, string method, object[] args);

        Task SendUserAsync(string userId, string method, object[] args);

        Task SendUsersAsync(IReadOnlyList<string> userIds, string method, object[] args);

        Task SendUsersExceptAsync(IReadOnlyList<string> userIds, string method, object[] args, IReadOnlyList<string> excludedConnectionIds);

        Task DeactiveClientAsync(string connectionId);

        Task DeactiveGroupAsync(string groupName);

        Task DeactiveUserAsync(string userId);
    }
}

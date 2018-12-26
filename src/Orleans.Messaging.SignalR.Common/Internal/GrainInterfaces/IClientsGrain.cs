using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Orleans.Messaging.SignalR.Internal
{
    public interface IClientsGrain : IGrainWithHubTypedStringKey
    {
        Task AddToClientsAsync(string connectionId);

        Task RemoveFromClientsAsync(string connectionId);

        [AlwaysInterleave]
        Task SendConnectionsAsync(string methodName, object[] args);

        [AlwaysInterleave]
        Task SendConnectionsExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds);

        Task DeactivateAsync();
    }
}

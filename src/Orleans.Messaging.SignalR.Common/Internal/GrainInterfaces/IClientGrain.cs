using Orleans.Concurrency;
using System;
using System.Threading.Tasks;

namespace Orleans.Messaging.SignalR.Internal
{
    public interface IClientGrain : IGrainWithHubTypedStringKey
    {
        Task OnConnectedAsync(Guid managerId);

        Task OnDisconnectedAsync();

        [AlwaysInterleave]
        Task SendConnectionAsync(string methodName, object[] args);

        Task DeactivateAsync();
    }
}

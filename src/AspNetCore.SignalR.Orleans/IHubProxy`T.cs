using Microsoft.AspNetCore.SignalR;

namespace Orleans.Messaging.SignalR
{
    public interface IHubProxy<THub> : IHubProxy where THub : Hub
    {
    }
}

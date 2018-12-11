using System;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    public interface IHubLifetimeManagerGrain : IGrainWithHubTypedGuidKey
    {
        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task OnHeartbeatAsync();

        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task OnInitializeAsync(TimeSpan timeoutInterval);

        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task OnConnectedAsync(string connectionId);

        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task OnDisconnectedAsync(string connectionId);

        /// <summary>
        /// Called by HubLifetimeManager.
        /// </summary>
        Task AbortAsync();
    }
}

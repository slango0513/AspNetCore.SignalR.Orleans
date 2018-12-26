using Microsoft.AspNetCore.SignalR;

namespace Orleans.Messaging.SignalR
{
    public class HubProxy<THub> : HubProxy, IHubProxy<THub> where THub : Hub
    {
        public HubProxy(IClusterClient clusterClient)
            : base(clusterClient, clusterClient.GetStreamProvider(SignalRConstants.STREAM_PROVIDER), typeof(THub).GUID)
        {
        }
    }
}

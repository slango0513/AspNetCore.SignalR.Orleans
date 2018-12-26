using Microsoft.AspNetCore.SignalR;
using Orleans.Messaging.SignalR;
using Sample.Abstractions;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SignalRSample.SignalRApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }

    // OrleansHubLifetimeManager<THub> uses typeof(THub).GUID to identify Hub types.
    // For small tests it returns consistent guids when the GuidAttribute is not associated,
    // but they should not be trusted to be stable over framework versions.
    // To be sure about consistency, explicitly decorating the Hub types with the GuidAttribute is recommend.
    // We can save them as constant in shared projects.
    [Guid(HubTypeIds.SampleHub)]
    public class SampleHub : Hub
    {
        private readonly IHubProxy<SampleHub> _hubProxy;
        private readonly IHubProxy<AnotherSampleHub> _anotherHubProxy;

        public SampleHub(IHubProxy<SampleHub> hubProxy, IHubProxy<AnotherSampleHub> anotherHubProxy)
        {
            _hubProxy = hubProxy;
            _anotherHubProxy = anotherHubProxy;
        }

        public Task NotifyUserFromAnotherHubAsync(string userId)
        {
            return _anotherHubProxy.SendUserAsync(userId, "OnReceived", new object[] { "Hello, user!" });
        }
    }

    [Guid(HubTypeIds.AnotherSampleHub)]
    public class AnotherSampleHub : Hub
    {
    }
}

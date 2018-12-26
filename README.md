# AspNetCore.SignalR.Orleans
Orleans for ASP.NET Core SignalR.
## Installation
In the client (SignalR) project via [NuGet](https://www.nuget.org/packages?q=aspnetcore.signalr.orleans):
```
PM> Install-Package AspNetCore.SignalR.Orleans
```
In the server (Orleans) project via [NuGet](https://www.nuget.org/packages?q=orleans.messaging.signalr):
```
PM> Install-Package Orleans.Messaging.SignalR
```
## Configuration
### Silo
ISiloHostBuilder:
```cs
    hostBuilder.UseSignalR();
```
### Client
IClientBuilder:
```cs
    clientBuilder.UseSignalR();
```
### SignalR
IServiceCollection:
```cs
     services.AddSignalR()
        .AddOrleans<SampleHub>(options => options.ClusterClient = clusterClient)
        .AddOrleans<AnotherSampleHub>(options => options.ClusterClient = clusterClient);
```
## Samples
### Orleans Grains
As shown in the example below, IHubProxy provides control over different channels on the Grain level.
```cs
    public class SampleGrainState
    {
        public ISet<string> Members { get; set; } = new HashSet<string>();
    }

    public class SampleGrain : Grain<SampleGrainState>, ISampleGrain
    {
        private IHubProxy _hubProxy;
        private IHubProxy _anotherHubProxy;

        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(SignalRConstants.STREAM_PROVIDER);
            _hubProxy = GrainFactory.GetHubProxy(streamProvider, Guid.Parse(HubTypeIds.SampleHub));
            _anotherHubProxy = GrainFactory.GetHubProxy(streamProvider, Guid.Parse(HubTypeIds.AnotherSampleHub));
            return base.OnActivateAsync();
        }

        public async Task AddToGroupWithConditionAsync(string connectionId, string groupName)
        {
            if (State.Members.Count <= 4)
            {
                await _hubProxy.AddToGroupAsync(connectionId, groupName);
                State.Members.Add(connectionId);
            }
        }

        public Task NotifyAllExceptCurrentMembersAsync()
        {
            return _hubProxy.SendAllExceptAsync("OnReceived", new object[] { "Hello, Client!" }, State.Members.ToList());
        }

        public Task NotifyUserFromAnotherHubAsync(string userId)
        {
            return _anotherHubProxy.SendUserAsync(userId, "OnReceived", new object[] { "Hello, User!" });
        }
    }
```
### SignalR Hubs
The following example shows how to un/subscribe/publish to channels across Hubs, and to generate a Hub type ID that is consistent across any physical machine.
```cs
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
```
### Shared
```cs
    public class HubTypeIds
    {
        public const string SampleHub = "85DE337C-0EBB-4DF5-9AA6-58E3503C5542";

        public const string AnotherSampleHub = "2FC4CAD6-A545-47FB-9D21-AD1606F7115A";
    }
```
## IHubProxy, generic IHubProxy and Hub.Clients/Hub.Groups, what is the difference?
The result of calling both interfaces (either from Hubs or Grains) and the own methods of SignalR Hub is identical.
For convenience, it is recommended to use the pattern in the samples above to get the IHubProxy: in SignalR projects getting from the construction process, in Orleans projects during the grain activation.
```cs
    public interface IHubProxy
    {
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
    }

    // From Microsoft.AspNetCore.SignalR.Core
    public abstract class Hub : IDisposable
    {
        public IHubCallerClients Clients { get; set; }

        public IGroupManager Groups { get; set; }
    }
```

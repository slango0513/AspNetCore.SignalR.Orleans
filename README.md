# AspNetCore.SignalR.Orleans
Orleans for ASP.NET Core SignalR.
## Samples
### SignalR Hubs
```cs
// Important. We can save it as constant anywhere.
[Guid("85DE337C-0EBB-4DF5-9AA6-58E3503C5542")]
public class SampleHub : Hub
{
    private readonly IClusterClient _clusterClient;
    private readonly IClientSetPartitioner<IGroupPartitionGrain> _groupPartitioner;
    private readonly IClientSetPartitioner<IUserPartitionGrain> _userPartitioner;

    public SampleHub(IOptions<OrleansOptions<SampleHub>> options,
        IClientSetPartitioner<IGroupPartitionGrain> groupPartitioner,
        IClientSetPartitioner<IUserPartitionGrain> userPartitioner)
    {
        _clusterClient = options.Value.ClusterClient;
        _groupPartitioner = groupPartitioner;
        _userPartitioner = userPartitioner;
    }

    public async Task<IEnumerable<string>> JoinGroupAsync(string groupName)
    {
        // Get clients in ths group, if members count less equals 4 then add him.
        var clientsInGroup = await _clusterClient.GetGroup<SampleHub>(groupName).GetConnectionIdsAsync();
        // Do not do this, just identical above. We can also get the members from another hub in this way.
        clientsInGroup = await _clusterClient.GetGroup(groupName, typeof(SampleHub).GUID).GetConnectionIdsAsync();
        if (clientsInGroup.Count <= 4)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            // Returns the current group members.
            return clientsInGroup;
        }
        else
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetCurrentJoinedGroupsAsync()
    {
        return await _clusterClient.GetClient<SampleHub>(Context.ConnectionId).GetGroupNamesAsync();
    }

    public async Task<IEnumerable<string>> GetAllGroupsAsync()
    {
        return await _groupPartitioner.GetAllClientSetIdsAsync(_clusterClient, typeof(SampleHub).GUID);
    }

    public async Task NotifyAllUsersFromAnotherHubAsync()
    {
        // Get all users from AnotherHub, and send them message.
        var allUsers = await _userPartitioner.GetAllClientSetIdsAsync(_clusterClient, typeof(AnotherHub).GUID);
        await Clients.Users(allUsers.ToList()).SendAsync("OnReceived", new object[] { "Hello, user!" });
    }
}

[Guid("2FC4CAD6-A545-47FB-9D21-AD1606F7115A")]
public class AnotherHub : Hub
{

}
```
### Orleans Grains
```cs
public class SampleGrain : Grain<SampleGrainState>, ISampleGrain
{
    private readonly IClientSetPartitioner<IGroupPartitionGrain> _groupPartitioner;
    private readonly IClientSetPartitioner<IUserPartitionGrain> _userPartitioner;

    public SampleGrain(IClientSetPartitioner<IGroupPartitionGrain> groupPartitioner,
        IClientSetPartitioner<IUserPartitionGrain> userPartitioner)
    {
        _groupPartitioner = groupPartitioner;
        _userPartitioner = userPartitioner;
    }

    public async Task NotifyClientsInGroupAsync(string groupName)
    {
        // Orleans silo side, first we get all signalr clients in group,
        var connectionIds = await GrainFactory.GetGroup(groupName, HubTypeIds.SampleHub).GetConnectionIdsAsync();

        // and send them messages.
        var tasks = connectionIds.Select(id => GrainFactory.GetClient(id, HubTypeIds.SampleHub).SendConnectionAsync("OnReceive", new object[] { "Hello, group member!" }));
        await Task.WhenAll(tasks);
    }

    public async Task SaveSnapshotAllGroupsAsync()
    {
        // Get all group names,
        var allGroupNames = await _groupPartitioner.GetAllClientSetIdsAsync(GrainFactory, HubTypeIds.SampleHub);

        // save with timestamp.
        State.AllGroups = new List<string>(allGroupNames);
        State.Timestamp = DateTime.UtcNow;
        await WriteStateAsync();
    }

    public async Task NotifyClientsAllUsersAsync()
    {
        // Get all user ids, but this time from AnotherSampleHub,
        var allUserIds = await _userPartitioner.GetAllClientSetIdsAsync(GrainFactory, HubTypeIds.AnotherSampleHub);

        // and send all clients message.
        var tasks = allUserIds.Select(id => GrainFactory.GetUser(id, HubTypeIds.AnotherSampleHub).SendClientSetAsync("OnReveive", new object[] { "Hello, user!" }));
        await Task.WhenAll(tasks);
    }
}

public class HubTypeIds
{
    // Euqals to typeof(SampleHub).GUID.
    public static readonly Guid SampleHub = Guid.Parse("85DE337C-0EBB-4DF5-9AA6-58E3503C5542");

    public static readonly Guid AnotherSampleHub = Guid.Parse("2FC4CAD6-A545-47FB-9D21-AD1606F7115A");
}
```
## Configuration
### Silo
ISiloHostBuilder:
```cs
builder.AddSignalR(); 
```
### Client
IClientBuilder:
```cs
builder.AddSignalR(); 
```
### SignalR
IServiceCollection:
```cs
service.AddSignalR()
.AddOrleans<MyHub>(options =>
    {
        options.ClusterClient = clusterClient;
    })
    .AddOrleans<AnotherHub>(options =>
    {
        options.ClusterClient = clusterClient;
    });
```

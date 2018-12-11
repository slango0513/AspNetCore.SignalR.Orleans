using AspNetCore.SignalR.Orleans;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SignalRSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }

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
}

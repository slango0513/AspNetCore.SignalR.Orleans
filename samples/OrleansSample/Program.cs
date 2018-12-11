using AspNetCore.SignalR.Orleans;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrleansSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }

    public interface ISampleGrain : IGrainWithStringKey
    {
    }

    public class SampleGrainState
    {
        public List<string> AllGroups = new List<string>();

        public DateTime Timestamp = DateTime.MinValue;
    }


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
}

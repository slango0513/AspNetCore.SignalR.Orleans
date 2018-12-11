using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AspNetCore.SignalR.Orleans.Tests
{
    public partial class OrleansHubLifetimeManagerTests : IClassFixture<TestClusterFixture>
    {
        private readonly TestClusterFixture _fixture;
        private readonly ITestOutputHelper _output;

        public OrleansHubLifetimeManagerTests(TestClusterFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        public OrleansHubLifetimeManager<THub> CreateNewHubLifetimeManager<THub>() where THub : Hub
        {
            var groupPartitioner = new DefaultClientSetPartitioner<IGroupPartitionGrain>();
            var userPartitioner = new DefaultClientSetPartitioner<IUserPartitionGrain>();
            var options = new OrleansOptions<THub> { ClusterClient = _fixture.TestCluster.Client };
            return new OrleansHubLifetimeManager<THub>(Options.Create(options),
                groupPartitioner,
                userPartitioner,
                new DefaultUserIdProvider(),
                NullLogger<OrleansHubLifetimeManager<THub>>.Instance);
        }

        [Fact]
        public async Task GetAllGroupNames()
        {
            using (var manager1 = CreateNewHubLifetimeManager<MyHub>())
            using (var manager2 = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient())
            using (var client2 = new TestClient())
            using (var client3 = new TestClient())
            {
                var groupPartitioner = new DefaultClientSetPartitioner<IGroupPartitionGrain>();

                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                var connection3 = HubConnectionContextUtils.Create(client2.Connection);

                await manager2.OnConnectedAsync(connection1).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();
                await manager1.OnConnectedAsync(connection3).OrTimeout();

                await manager1.AddToGroupAsync(connection1.ConnectionId, "group").OrTimeout();
                await manager1.AddToGroupAsync(connection2.ConnectionId, "group").OrTimeout();
                await manager2.AddToGroupAsync(connection3.ConnectionId, "group2").OrTimeout();

                var result = await groupPartitioner.GetAllClientSetIdsAsync(_fixture.TestCluster.Client, typeof(MyHub).GUID).OrTimeout();
                Assert.Equal(2, result.Count());

                await manager1.OnDisconnectedAsync(connection2).OrTimeout();

                // TODO
                //var result1 = await groupPartitioner.GetClientSetIds(_fixture.TestCluster.Client).OrTimeout();
                //Assert.Equal(2, result1.Count());

                await manager2.OnDisconnectedAsync(connection3).OrTimeout();

                var result2 = await groupPartitioner.GetAllClientSetIdsAsync(_fixture.TestCluster.Client, typeof(MyHub).GUID).OrTimeout();
                Assert.Single(result2);
            }
        }

        [Fact]
        public async Task GetAllUserIds()
        {
            using (var manager1 = CreateNewHubLifetimeManager<MyHub>())
            using (var manager2 = CreateNewHubLifetimeManager<MyHub>())
            using (var client1 = new TestClient(userIdentifier: "userA"))
            using (var client2 = new TestClient(userIdentifier: "userA"))
            using (var client3 = new TestClient(userIdentifier: "userB"))
            {
                var userPartitioner = new DefaultClientSetPartitioner<IUserPartitionGrain>();

                var connection1 = HubConnectionContextUtils.Create(client1.Connection);
                var connection2 = HubConnectionContextUtils.Create(client2.Connection);
                var connection3 = HubConnectionContextUtils.Create(client3.Connection);

                await manager1.OnConnectedAsync(connection1).OrTimeout();
                await manager1.OnConnectedAsync(connection3).OrTimeout();
                await manager2.OnConnectedAsync(connection2).OrTimeout();

                await manager2.OnDisconnectedAsync(connection2).OrTimeout();

                var result1 = await userPartitioner.GetAllClientSetIdsAsync(_fixture.TestCluster.Client, typeof(MyHub).GUID).OrTimeout();
                Assert.Equal(2, result1.Count());

                await manager1.OnDisconnectedAsync(connection3).OrTimeout();

                var result2 = await userPartitioner.GetAllClientSetIdsAsync(_fixture.TestCluster.Client, typeof(MyHub).GUID).OrTimeout();
                Assert.Single(result2);
            }
        }
    }
}

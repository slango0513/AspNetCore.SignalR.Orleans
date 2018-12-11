using AspNetCore.SignalR.Orleans;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.TestingHost;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Playground
{
    public class LineReadingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<OrleansOptions<SampleHub>> _options;
        private readonly TestCluster _testCluster;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;

        public LineReadingService(IServiceProvider serviceProvider,
            IOptions<OrleansOptions<SampleHub>> options,
            TestCluster testCluster,
            ILoggerFactory loggerFactory,
            ILogger<LineReadingService> logger)
        {
            _serviceProvider = serviceProvider;
            _options = options;
            _testCluster = testCluster;
            _loggerFactory = loggerFactory;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(async () =>
            {
                foreach (var item in clientReader.Values)
                {
                    item.Dispose();
                }
                foreach (var item in testClients.Values)
                {
                    item.Abort();
                }
                foreach (var item in managers.Values)
                {
                    item.Dispose();
                }
                await _testCluster.Client.Close();

                _logger.LogInformation(2, "stopped.");
            });



            Observable.FromAsync(() => Console.In.ReadLineAsync())
                .Repeat()
                .Where(value => !string.IsNullOrEmpty(value))
                .Select(value => value.Split(' '))
                .SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(async (string[] value) =>
                {
                    await Task.CompletedTask;

                    try
                    {
                        await OnNextAsync(value);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception onNext.");
                    }
                },
                error => _logger.LogError(error, "OnError."),
                () => _logger.LogInformation("OnCompleted."));

            return Task.CompletedTask;
        }

        private readonly ConcurrentDictionary<string, OrleansHubLifetimeManager<SampleHub>> managers
            = new ConcurrentDictionary<string, OrleansHubLifetimeManager<SampleHub>>();
        private IClientSetPartitioner<IGroupPartitionGrain> _groupPartitioner;
        private IClientSetPartitioner<IUserPartitionGrain> _userPartitioner;

        private readonly ConcurrentDictionary<string, HubConnectionContext> testClients
            = new ConcurrentDictionary<string, HubConnectionContext>();
        private readonly ConcurrentDictionary<string, IDisposable> clientReader
            = new ConcurrentDictionary<string, IDisposable>();

        private async Task CreateManagerAsync(string managerId)
        {
            var options = new OrleansOptions<SampleHub>
            {
                ClusterClient = _testCluster.Client,
                TimeoutInterval = TimeSpan.FromSeconds(15)
            };
            _groupPartitioner = new DefaultClientSetPartitioner<IGroupPartitionGrain>();
            _userPartitioner = new DefaultClientSetPartitioner<IUserPartitionGrain>();
            var manager = new OrleansHubLifetimeManager<SampleHub>(Options.Create(options),
                _groupPartitioner,
                _userPartitioner,
                new DefaultUserIdProvider(),
                _loggerFactory.CreateLogger<OrleansHubLifetimeManager<SampleHub>>());
            await manager.ConnectToClusterAsync();
            managers[managerId] = manager;
        }

        private async Task CreateClientAsync(string managerId, string connectionId, string userId)
        {

            var client = new TestClient(userIdentifier: userId);

            var connection = HubConnectionContextUtils.Create(client.Connection);
            testClients[connectionId] = connection;

            await managers[managerId].OnConnectedAsync(connection);

            var reader = Observable.Repeat(Observable.FromAsync(() => client.ReadAsync()))
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(message => message as InvocationMessage)
            .Subscribe(message =>
            {
                _logger.LogInformation($"Method: {message.Target}, Args: {JsonConvert.SerializeObject(message.Arguments)}");
            });
            clientReader[connectionId] = reader;
        }

        private async Task OnNextAsync(string[] value)
        {
            switch (value[0])
            {
                case "m":
                    {
                        var managerId = value[1];
                        await CreateManagerAsync(managerId);
                    }
                    break;
                case "c":
                    {
                        var managerId = value[1];
                        var connectionId = value[2];
                        var userId = value[3];
                        await CreateClientAsync(managerId, connectionId, userId);
                    }
                    break;
                case "ms":
                    {
                        var managerId = value[1];
                        Parallel.For(0, 10, async i =>
                        {
                            await CreateManagerAsync($"{managerId}{i}");
                        });
                    }
                    break;
                case "cu":
                    {
                        var connectionId = value[1];
                        var userId = value[2];

                        Parallel.ForEach(managers.Keys, async managerId =>
                        {
                            var clientTasks = Enumerable.Range(0, 20).Select(i => $"{connectionId}{i}").Select(id => CreateClientAsync(managerId, id, userId));
                            await Task.WhenAll(clientTasks);
                        });
                    }
                    break;
                case "dm":
                    {
                        var managerId = value[1];

                        managers[managerId].Dispose();
                    }
                    break;
                case "dc":
                    {
                        var managerId = value[1];
                        var connectionId = value[2];

                        await managers[managerId].OnDisconnectedAsync(testClients[connectionId]);
                    }
                    break;
                case "sa":
                    {
                        var managerId = value[1];

                        await managers[managerId].SendAllAsync("Hello", new object[] { "All" });
                    }
                    break;
                case "sg":
                    {
                        var managerId = value[1];
                        var groupName = value[2];

                        await managers[managerId].SendGroupAsync(groupName, "Hello", new object[] { "Group member" });
                    }
                    break;
                case "su":
                    {
                        var managerId = value[1];
                        var userId = value[2];

                        await managers[managerId].SendUserAsync(userId, "Hello", new object[] { "User client" });
                    }
                    break;
                case "atg":
                    {
                        var managerId = value[1];
                        var connectionId = value[2];
                        var groupName = value[3];

                        await managers[managerId].AddToGroupAsync(testClients[connectionId].ConnectionId, groupName);
                    }
                    break;
                case "rfg":
                    {
                        var managerId = value[1];
                        var connectionId = value[2];
                        var groupName = value[3];

                        await managers[managerId].RemoveFromGroupAsync(testClients[connectionId].ConnectionId, groupName);
                    }
                    break;
                case "ga":
                    {
                        var managerId = value[1];

                        var groups = await _groupPartitioner.GetAllClientSetIdsAsync(_testCluster.Client, typeof(SampleHub).GUID);
                        var groupClients = await Task.WhenAll(groups.Select(name => _testCluster.Client.GetGroup(name, typeof(SampleHub).GUID).GetConnectionIdsAsync()));
                        _logger.LogInformation($"GroupNames: {JsonConvert.SerializeObject(groups)}, Clients: {JsonConvert.SerializeObject(groupClients)}");

                        var users = await _userPartitioner.GetAllClientSetIdsAsync(_testCluster.Client, typeof(SampleHub).GUID);
                        var userClients = await Task.WhenAll(users.Select(id => _testCluster.Client.GetGroup(id, typeof(SampleHub).GUID).GetConnectionIdsAsync()));
                        _logger.LogInformation($"UserIds: {JsonConvert.SerializeObject(users)}, Clients: {JsonConvert.SerializeObject(userClients)}");
                    }
                    break;


                //case "sr":
                //    {
                //        var range = RangeFactory.CreateRange(uint.Parse(value[1]), uint.Parse(value[2]));
                //        var subRanges = RangeFactory.GetSubRanges(range);
                //        _logger.LogInformation($"subRanges: {JsonConvert.SerializeObject(subRanges)}");



                //        //var id = Utils.CalculateIdHash(value[1]);
                //        //_logger.LogInformation($"id: {id}");
                //    }
                //    break;
                //case "ifr":
                //    {
                //        var fullRange = RangeFactory.CreateFullRange();
                //        _logger.LogInformation($"{value[1]} in full range: {fullRange.InRange(uint.Parse(value[1]))}");
                //    }
                //    break;
                //case "ir":
                //    {
                //        var range = RangeFactory.CreateRange(uint.Parse(value[1]), uint.Parse(value[2]));
                //        _logger.LogInformation($"{value[3]} in range: {range.InRange(uint.Parse(value[3]))}");
                //    }
                //    break;
                default:
                    break;
            }
        }
    }

    public static partial class TestClientExtensions
    {
        public static HubConnectionContext CreateConnection(this TestClient testClient, IHubProtocol protocol = null, string userIdentifier = null)
        {
            return HubConnectionContextUtils.Create(testClient.Connection, protocol, userIdentifier);
        }
    }

    public class SampleHub : Hub { }
}


// GetGrainIdentity()
// *grn/DDCE39C2/00000000
// +
// GorfHye1QDl -t7eN7jufAg:THub // Key

// IdentityString
// *grn/DDCE39C2/0000000000000000000000000000000006ffffff
// ddce39c2
// +
// GorfHye1QDl-t7eN7jufAg:THub // Key
// -
// 0xCB39546E // UniformHashCode

// RuntimeIdentity
// S127.0.0.1:11111:281586790

// TypeCode
// -573687358

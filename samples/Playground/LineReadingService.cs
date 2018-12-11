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
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Samples
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

        private readonly Dictionary<string, OrleansHubLifetimeManager<SampleHub>> managers
            = new Dictionary<string, OrleansHubLifetimeManager<SampleHub>>();
        private readonly Dictionary<string, IClientSetPartitioner<IGroupPartitionGrain>> groupPartitioners
            = new Dictionary<string, IClientSetPartitioner<IGroupPartitionGrain>>();
        private readonly Dictionary<string, IClientSetPartitioner<IUserPartitionGrain>> userPartitioners
            = new Dictionary<string, IClientSetPartitioner<IUserPartitionGrain>>();

        private readonly Dictionary<string, HubConnectionContext> testClients = new Dictionary<string, HubConnectionContext>();
        private readonly Dictionary<string, IDisposable> clientDisposables = new Dictionary<string, IDisposable>();

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(async () =>
            {
                foreach (var item in clientDisposables.Values)
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
                        switch (value[0])
                        {
                            case "m":
                                {
                                    var managerId = value[1];

                                    var groupPartitioner = new DefaultClientSetPartitioner<IGroupPartitionGrain>();
                                    var userPartitioner = new DefaultClientSetPartitioner<IUserPartitionGrain>();
                                    managers[managerId] = new OrleansHubLifetimeManager<SampleHub>(Options.Create(new OrleansOptions<SampleHub> { ClusterClient = _testCluster.Client }),
                                        groupPartitioner,
                                        userPartitioner,
                                        new DefaultUserIdProvider(),
                                        _loggerFactory.CreateLogger<OrleansHubLifetimeManager<SampleHub>>());

                                    groupPartitioners[managerId] = groupPartitioner;
                                    userPartitioners[managerId] = userPartitioner;
                                }
                                break;
                            case "dm":
                                {
                                    var managerId = value[1];

                                    managers[managerId].Dispose();
                                }
                                break;
                            case "sa":
                                {
                                    var managerId = value[1];

                                    await managers[managerId].SendAllAsync("Hello", new object[] { "World" });
                                }
                                break;
                            case "sg":
                                {
                                    var managerId = value[1];
                                    var groupName = value[2];

                                    await managers[managerId].SendGroupAsync(groupName, "Hello", new object[] { "World" });
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
                            case "c":
                                {
                                    var managerId = value[1];
                                    var connectionId = value[2];
                                    var userId = value[3];

                                    var client = new TestClient(userIdentifier: userId);

                                    var connection = HubConnectionContextUtils.Create(client.Connection);
                                    testClients[connectionId] = connection;

                                    await managers[managerId].OnConnectedAsync(connection);

                                    var disposable = Observable.Repeat(Observable.FromAsync(() => client.ReadAsync()))
                                    .ObserveOn(TaskPoolScheduler.Default)
                                    .Subscribe(message =>
                                    {
                                        var _message = message as InvocationMessage;
                                        _logger.LogInformation($"Method {_message.Target}, Args {JsonConvert.SerializeObject(_message.Arguments)}");
                                    });
                                    clientDisposables[connectionId] = disposable;
                                }
                                break;
                            case "ga":
                                {
                                    var managerId = value[1];

                                    var result2 = await groupPartitioners[managerId].GetAllClientSetIdsAsync(_testCluster.Client, typeof(SampleHub).GUID);
                                    var result3 = await userPartitioners[managerId].GetAllClientSetIdsAsync(_testCluster.Client, typeof(SampleHub).GUID);
                                    _logger.LogInformation($"GroupNames: {JsonConvert.SerializeObject(result2)}, " +
                                        $"UserIds: {JsonConvert.SerializeObject(result3)}");
                                }
                                break;
                            case "dc":
                                {
                                    var managerId = value[1];
                                    var connectionId = value[2];

                                    await managers[managerId].OnDisconnectedAsync(testClients[connectionId]);
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
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception onNext.");
                    }
                },
                error => _logger.LogError(error, "OnError."),
                () => _logger.LogInformation("OnCompleted."));

            return Task.CompletedTask;
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

using AspNetCore.SignalR.Orleans;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Messaging.SignalR;
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
                foreach (var item in testClients.Values)
                {
                    item.Connection.Abort();
                    item.Reader.Dispose();
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

        private readonly ConcurrentDictionary<string, OrleansHubLifetimeManager<SampleHub>> managers = new ConcurrentDictionary<string, OrleansHubLifetimeManager<SampleHub>>();

        public class ReadableClient
        {
            public HubConnectionContext Connection { get; set; }

            public IDisposable Reader { get; set; }
        }

        private readonly ConcurrentDictionary<string, ReadableClient> testClients = new ConcurrentDictionary<string, ReadableClient>();

        private async Task CreateManagerAsync(string managerId)
        {
            var options = new OrleansOptions<SampleHub>
            {
                ClusterClient = _testCluster.Client
            };

            var manager = new OrleansHubLifetimeManager<SampleHub>(Options.Create(options),
                new HubProxy<SampleHub>(options.ClusterClient),
                _loggerFactory.CreateLogger<OrleansHubLifetimeManager<SampleHub>>());

            managers[managerId] = manager;

            await Task.CompletedTask;
        }

        private async Task CreateClientAsync(string managerId, string connectionId, string userId)
        {
            var client = new TestClient(connectionId: connectionId);

            var connection = HubConnectionContextUtils.Create(client.Connection, userIdentifier: userId);

            await managers[managerId].OnConnectedAsync(connection);

            var reader = Observable.Repeat(Observable.FromAsync(() => client.ReadAsync()))
            .ObserveOn(TaskPoolScheduler.Default)
            .Select(message => message as InvocationMessage)
            .Subscribe(message =>
            {
                _logger.LogInformation($"Method: {message.Target}, Args: {JsonConvert.SerializeObject(message.Arguments)}");
            });

            testClients[connectionId] = new ReadableClient { Connection = connection, Reader = reader };
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
                        Parallel.For(0, 3, async i =>
                        {
                            await CreateManagerAsync($"{i}");
                        });
                    }
                    break;
                case "cs":
                    {
                        var userId = value[1];

                        Parallel.ForEach(managers.Keys, async managerId =>
                        {
                            var clientTasks = Enumerable.Range(0, 3).Select(i => $"{Guid.NewGuid()}").Select(id => CreateClientAsync(managerId, id, userId));
                            await Task.WhenAll(clientTasks);
                        });
                    }
                    break;
                case "atgs":
                    {
                        var groupName = value[1];

                        Parallel.ForEach(managers, manager =>
                        {
                            Parallel.ForEach(testClients, client =>
                            {
                                manager.Value.AddToGroupAsync(client.Value.Connection.ConnectionId, groupName);
                            });
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

                        await managers[managerId].OnDisconnectedAsync(testClients[connectionId].Connection);
                    }
                    break;
                case "dac":
                    {
                        var managerId = value[1];
                        var connectionId = value[21];

                        await managers[managerId].HubProxy.DeactiveClientAsync(connectionId);
                    }
                    break;
                case "dag":
                    {
                        var managerId = value[1];
                        var groupName = value[2];

                        await managers[managerId].HubProxy.DeactiveGroupAsync(groupName);
                    }
                    break;
                case "dau":
                    {
                        var managerId = value[1];
                        var userId = value[2];

                        await managers[managerId].HubProxy.DeactiveUserAsync(userId);
                    }
                    break;
                case "sa":
                    {
                        var managerId = value[1];

                        await managers[managerId].SendAllAsync("Hello", new object[] { "All" });
                        _logger.LogInformation("Done");
                    }
                    break;
                case "sg":
                    {
                        var managerId = value[1];
                        var groupName = value[2];

                        await managers[managerId].SendGroupAsync(groupName, "Hello", new object[] { "Group member" });
                        _logger.LogInformation("Done");
                    }
                    break;
                case "su":
                    {
                        var managerId = value[1];
                        var userId = value[2];

                        await managers[managerId].SendUserAsync(userId, "Hello", new object[] { "User client" });
                        _logger.LogInformation("Done");
                    }
                    break;
                case "atg":
                    {
                        var managerId = value[1];
                        var connectionId = value[2];
                        var groupName = value[3];

                        await managers[managerId].AddToGroupAsync(testClients[connectionId].Connection.ConnectionId, groupName);
                    }
                    break;
                case "rfg":
                    {
                        var managerId = value[1];
                        var connectionId = value[2];
                        var groupName = value[3];

                        await managers[managerId].RemoveFromGroupAsync(testClients[connectionId].Connection.ConnectionId, groupName);
                    }
                    break;
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

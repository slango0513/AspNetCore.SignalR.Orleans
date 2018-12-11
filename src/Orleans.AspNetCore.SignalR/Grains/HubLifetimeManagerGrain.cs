using Microsoft.Extensions.Logging;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal class HubLifetimeManagerGrainState
    {
        public HashSet<string> ConnectionIds { get; set; } = new HashSet<string>();

        public TimeSpan TimeoutInterval { get; set; } = TimeSpan.MinValue;

        public DateTime LastReceivedTimeStamp { get; set; } = DateTime.MinValue;

        public bool ReceivedMessageThisInterval { get; set; } = false;

        public bool Aborted { get; set; } = false;
    }

    //[StorageProvider(ProviderName = Constants.STORAGE_PROVIDER)]
    internal class HubLifetimeManagerGrain : Grain, IHubLifetimeManagerGrain
    {
        private readonly ILogger _logger;

        public HubLifetimeManagerGrain(ILogger<HubLifetimeManagerGrain> logger)
        {
            _logger = logger;
        }

        // TODO
        public HubLifetimeManagerGrainState State { get; set; } = new HubLifetimeManagerGrainState();

        private Guid _hubLifetimeManagerId;
        private Guid _hubTypeId;
        private IDisposable _checkTimeoutTimer;

        public override async Task OnActivateAsync()
        {
            _hubLifetimeManagerId = this.GetId();
            _hubTypeId = this.GetHubTypeId();

            if (!State.Aborted)
            {
                await ResetTimeoutAsync();
                _checkTimeoutTimer = RegisterTimer(_ => CheckClientTimeoutAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
            }
        }

        private async Task CheckClientTimeoutAsync()
        {
            // If it's been too long since we've heard from the server
            var duration = DateTime.UtcNow - State.LastReceivedTimeStamp;
            if (duration > State.TimeoutInterval)
            {
                if (!State.ReceivedMessageThisInterval)
                {
                    Log.Timeout(_logger, State.TimeoutInterval);
                    await AbortAsync();
                }

                State.ReceivedMessageThisInterval = false;
                State.LastReceivedTimeStamp = DateTime.UtcNow;
            }
        }

        public async Task AbortAsync()
        {
            _checkTimeoutTimer.Dispose();
            try
            {
                var clientTasks = State.ConnectionIds.Select(id => GrainFactory.GetClientGrain(id, _hubTypeId).OnDisconnectedAsync());
                await Task.WhenAll(clientTasks);
            }
            catch (Exception ex)
            {
                Log.AbortFailed(_logger, ex);
            }

            State.Aborted = true;
            DeactivateOnIdle();
        }

        public Task OnInitializeAsync(TimeSpan timeoutInterval)
        {
            State.TimeoutInterval = timeoutInterval;
            return ResetTimeoutAsync();
        }

        public async Task OnConnectedAsync(string connectionId)
        {
            await ResetTimeoutAsync();
            await GrainFactory.GetClientGrain(connectionId, _hubTypeId).OnConnectedAsync(_hubLifetimeManagerId);

            if (State.ConnectionIds.Add(connectionId))
            {
            }
        }

        public async Task OnDisconnectedAsync(string connectionId)
        {
            await ResetTimeoutAsync();
            await GrainFactory.GetClientGrain(connectionId, _hubTypeId).OnDisconnectedAsync();

            if (State.ConnectionIds.Remove(connectionId))
            {
            }
        }

        public Task OnHeartbeatAsync()
        {
            return ResetTimeoutAsync();
        }

        private Task ResetTimeoutAsync()
        {
            State.ReceivedMessageThisInterval = true;
            return Task.CompletedTask;
        }

        private static class Log
        {
            private static readonly Action<ILogger, Exception> _abortFailed =
                LoggerMessage.Define(LogLevel.Trace, new EventId(1, "AbortFailed"), "Abort callback failed.");

            private static readonly Action<ILogger, int, Exception> _Timeout =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2, "Timeout"), "Timeout ({Timeout}ms) elapsed without receiving a message from the server.");


            public static void AbortFailed(ILogger logger, Exception exception)
            {
                _abortFailed(logger, exception);
            }

            public static void Timeout(ILogger logger, TimeSpan timeout)
            {
                _Timeout(logger, (int)timeout.TotalMilliseconds, null);
            }
        }
    }
}

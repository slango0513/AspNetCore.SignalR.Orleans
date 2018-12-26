using Microsoft.Extensions.Logging;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orleans.Messaging.SignalR.Internal
{
    internal abstract class ClientsGrainState
    {
        public Dictionary<string, StreamSubscriptionHandle<EventArgs>> HandlesByConnectionId { get; set; }
            = new Dictionary<string, StreamSubscriptionHandle<EventArgs>>();
    }

    internal abstract class ClientsGrain<TGrainState> : Grain<TGrainState>, IClientsGrain
        where TGrainState : ClientsGrainState, new()
    {
        protected readonly ILogger _logger;

        public ClientsGrain(ILogger logger) : base()
        {
            _logger = logger;
        }

        public override async Task OnActivateAsync()
        {
            var tasks = State.HandlesByConnectionId.Keys.Select(id => ResumeRemoveHandleAsync(id));
            await Task.WhenAll(tasks);

            async Task ResumeRemoveHandleAsync(string connectionId)
            {
                var handles = await GetStreamProvider(SignalRConstants.STREAM_PROVIDER)
                    .GetStream<EventArgs>(InternalSignalRConstants.DISCONNECTION_STREAM_ID, connectionId)
                    .GetAllSubscriptionHandles();
                var _tasks = handles.Select(handle => handle.ResumeAsync(async (_, token) => await RemoveFromClientsAsync(connectionId)));
                await Task.WhenAll(_tasks);
            }
        }

        public virtual async Task AddToClientsAsync(string connectionId)
        {
            if (!State.HandlesByConnectionId.ContainsKey(connectionId))
            {
                var handle = await GetStreamProvider(SignalRConstants.STREAM_PROVIDER)
                    .GetStream<EventArgs>(InternalSignalRConstants.DISCONNECTION_STREAM_ID, connectionId)
                    .SubscribeAsync(async (_, token) => await RemoveFromClientsAsync(connectionId));
                State.HandlesByConnectionId.Add(connectionId, handle);
                await WriteStateAsync();
            }
        }

        public virtual async Task RemoveFromClientsAsync(string connectionId)
        {
            if (State.HandlesByConnectionId.ContainsKey(connectionId))
            {
                var handle = State.HandlesByConnectionId[connectionId];
                await handle.UnsubscribeAsync();
                State.HandlesByConnectionId.Remove(connectionId);
                await WriteStateAsync();
            }

            if (State.HandlesByConnectionId.Count == 0)
            {
                DeactivateOnIdle();
            }
        }

        public virtual Task SendConnectionsAsync(string methodName, object[] args)
        {
            var hubTypeId = this.GetHubTypeId();
            var hubProxy = GrainFactory.GetHubProxy(GetStreamProvider(SignalRConstants.STREAM_PROVIDER), hubTypeId);
            return hubProxy.SendClientsAsync(State.HandlesByConnectionId.Keys.ToList(), methodName, args);
        }

        public virtual Task SendConnectionsExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            var hubTypeId = this.GetHubTypeId();
            var hubProxy = GrainFactory.GetHubProxy(GetStreamProvider(SignalRConstants.STREAM_PROVIDER), hubTypeId);
            return hubProxy.SendClientsExceptAsync(State.HandlesByConnectionId.Keys.ToList(), methodName, args, excludedConnectionIds);
        }

        public virtual Task DeactivateAsync()
        {
            DeactivateOnIdle();
            return Task.CompletedTask;
        }
    }
}

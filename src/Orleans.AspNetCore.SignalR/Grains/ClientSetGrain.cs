using Orleans;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal abstract class ClientSetGrainState
    {
        public HashSet<string> ConnectionIds { get; set; } = new HashSet<string>();
    }

    internal abstract class ClientSetGrain<TGrainState> : Grain<TGrainState>, IClientSetGrain
        where TGrainState : ClientSetGrainState, new()
    {
        public async Task<int> AddToClientSetAsync(string connectionId)
        {
            if (State.ConnectionIds.Add(connectionId))
            {
                await WriteStateAsync();
            }
            return State.ConnectionIds.Count;
        }

        public async Task<int> RemoveFromClientSetAsync(string connectionId)
        {
            if (State.ConnectionIds.Remove(connectionId))
            {
                await WriteStateAsync();
            }
            return State.ConnectionIds.Count;
        }

        public async Task<IReadOnlyCollection<string>> GetConnectionIdsAsync()
        {
            return await Task.FromResult(State.ConnectionIds);
        }

        public Task SendClientSetAsync(string methodName, object[] args)
        {
            return SendClientSetExceptAsync(methodName, args, Enumerable.Empty<string>().ToList());
        }

        public Task SendClientSetExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            var hubTypeId = this.GetHubTypeId();

            var tasks = State.ConnectionIds.Where(id => !excludedConnectionIds.Contains(id))
                .Select(id => GrainFactory.GetClient(id, hubTypeId).SendConnectionAsync(methodName, args));
            return Task.WhenAll(tasks);
        }
    }
}

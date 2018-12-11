using Orleans;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans.Internal
{
    internal abstract class ClientSetPartitionGrainState
    {
        public HashSet<string> ConnectionIds { get; set; } = new HashSet<string>();
    }

    internal abstract class ClientSetPartitionGrain<TGrainState> : Grain<TGrainState>, IClientSetPartitionGrain
        where TGrainState : ClientSetPartitionGrainState, new()
    {
        protected async Task AddToClientSetAsync(string connectionId, IClientSetGrain clientSet)
        {
            var count = await clientSet.AddToClientSetAsync(connectionId);
            if (count == 1)
            {
                var clientSetId = clientSet.GetId();
                if (State.ConnectionIds.Add(clientSetId))
                {
                    await WriteStateAsync();
                }
            }
        }

        protected async Task RemoveFromClientSetAsync(string connectionId, IClientSetGrain clientSet)
        {
            var count = await clientSet.RemoveFromClientSetAsync(connectionId);
            if (count == 0)
            {
                var clientSetId = clientSet.GetId();
                if (State.ConnectionIds.Remove(clientSetId))
                {
                    await WriteStateAsync();
                }
            }
        }

        public async Task<IReadOnlyCollection<string>> GetClientSetIdsAsync()
        {
            return await Task.FromResult(State.ConnectionIds);
        }
    }
}

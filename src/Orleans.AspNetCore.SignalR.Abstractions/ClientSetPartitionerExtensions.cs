using AspNetCore.SignalR.Orleans.Internal;
using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public static partial class ClientSetPartitionerExtensions
    {
        public static TGrainInterface GetPartitionGrain<TGrainInterface>(this IClientSetPartitioner<TGrainInterface> partitioner, IGrainFactory grainFactory, string clientSetId, Guid hubTypeId)
            where TGrainInterface : IClientSetPartitionGrain
        {
            var partitionId = partitioner.GetPartitionId(clientSetId, hubTypeId);
            return grainFactory.GetGrain<TGrainInterface>(HubTypedKeyUtils.ToHubTypedKeyString(partitionId, hubTypeId));
        }

        public static IEnumerable<TGrainInterface> GetAllPartitionGrains<TGrainInterface>(this IClientSetPartitioner<TGrainInterface> partitioner, IGrainFactory grainFactory, Guid hubTypeId)
            where TGrainInterface : IClientSetPartitionGrain
        {
            var partitionIds = partitioner.GetPartitionIds(hubTypeId);
            return partitionIds.Select(id => grainFactory.GetGrain<TGrainInterface>(HubTypedKeyUtils.ToHubTypedKeyString(id, hubTypeId)));
        }

        public static async Task<IEnumerable<string>> GetAllClientSetIdsAsync<TGrainInterface>(this IClientSetPartitioner<TGrainInterface> partitioner, IGrainFactory grainFactory, Guid hubTypeId)
            where TGrainInterface : IClientSetPartitionGrain
        {
            var tasks = partitioner.GetAllPartitionGrains(grainFactory, hubTypeId).Select(partition => partition.GetClientSetIdsAsync());
            var result = await Task.WhenAll(tasks);
            return result.SelectMany(_ => _);
        }
    }
}

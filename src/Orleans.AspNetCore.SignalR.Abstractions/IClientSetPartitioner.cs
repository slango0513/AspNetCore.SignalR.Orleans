using System;
using System.Collections.Generic;

namespace AspNetCore.SignalR.Orleans
{
    public interface IClientSetPartitioner<TGrainInterface> where TGrainInterface : IClientSetPartitionGrain
    {
        string GetPartitionId(string clientSetId, Guid hubTypeId);

        IEnumerable<string> GetPartitionIds(Guid hubTypeId);
    }
}

using AspNetCore.SignalR.Orleans.Internal;
using Orleans.Concurrency;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public interface IClientSetPartitionGrain : IGrainWithHubTypedStringKey
    {
        [AlwaysInterleave]
        Task<IReadOnlyCollection<string>> GetClientSetIdsAsync();
    }
}

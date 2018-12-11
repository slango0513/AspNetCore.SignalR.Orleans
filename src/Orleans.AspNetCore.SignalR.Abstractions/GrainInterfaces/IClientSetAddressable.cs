using Orleans.Concurrency;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public interface IClientSetAddressable : IAddressable
    {
        [AlwaysInterleave]
        Task<IReadOnlyCollection<string>> GetConnectionIdsAsync();

        [AlwaysInterleave]
        Task SendClientSetAsync(string methodName, object[] args);

        [AlwaysInterleave]
        Task SendClientSetExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds);
    }
}

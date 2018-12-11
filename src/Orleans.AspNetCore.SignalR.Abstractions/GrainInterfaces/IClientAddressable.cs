using Orleans.Concurrency;
using Orleans.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCore.SignalR.Orleans
{
    public interface IClientAddressable : IAddressable
    {
        [AlwaysInterleave]
        Task<ISet<string>> GetGroupNamesAsync();

        [AlwaysInterleave]
        Task<string> GetUserIdAsync();

        [AlwaysInterleave]
        Task SendConnectionAsync(string methodName, object[] args);
    }
}

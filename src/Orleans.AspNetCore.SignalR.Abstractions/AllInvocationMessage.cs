using System.Collections.Generic;

namespace AspNetCore.SignalR.Orleans
{
    public class AllInvocationMessage : HubInvocationMessage
    {
        public AllInvocationMessage(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
            : base(methodName, args)
        {
            ExcludedConnectionIds = excludedConnectionIds;
        }

        public IReadOnlyList<string> ExcludedConnectionIds { get; }
    }
}

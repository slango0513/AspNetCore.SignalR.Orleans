using System.Collections.Generic;

namespace Orleans.Messaging.SignalR
{
    public class SendAllInvocationMessage : HubInvocationMessage
    {
        public SendAllInvocationMessage(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
            : base(methodName, args)
        {
            ExcludedConnectionIds = excludedConnectionIds;
        }

        public IReadOnlyList<string> ExcludedConnectionIds { get; }
    }
}

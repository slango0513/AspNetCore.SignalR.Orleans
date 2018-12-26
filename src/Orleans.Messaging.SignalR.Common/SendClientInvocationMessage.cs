namespace Orleans.Messaging.SignalR.Internal
{
    public class SendClientInvocationMessage : HubInvocationMessage
    {
        public SendClientInvocationMessage(string methodName, object[] args, string connectionId)
            : base(methodName, args)
        {
            ConnectionId = connectionId;
        }

        public string ConnectionId { get; }
    }
}

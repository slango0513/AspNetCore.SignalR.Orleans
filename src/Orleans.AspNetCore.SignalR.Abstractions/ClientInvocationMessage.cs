namespace AspNetCore.SignalR.Orleans
{
    public class ClientInvocationMessage : HubInvocationMessage
    {
        public ClientInvocationMessage(string methodName, object[] args, string connectionId)
            : base(methodName, args)
        {
            ConnectionId = connectionId;
        }

        public string ConnectionId { get; }
    }
}

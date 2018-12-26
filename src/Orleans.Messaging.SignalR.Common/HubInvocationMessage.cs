namespace Orleans.Messaging.SignalR
{
    public abstract class HubInvocationMessage
    {
        public HubInvocationMessage(string methodName, object[] args)
        {
            MethodName = methodName;
            Args = args;
        }

        public string MethodName { get; }

        public object[] Args { get; }
    }
}

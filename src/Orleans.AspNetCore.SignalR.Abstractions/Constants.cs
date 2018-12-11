namespace AspNetCore.SignalR.Orleans
{
    public static class Constants
    {
        public const string PREFIX = "ASPNETCORE_SIGNALR_ORLEANS_";

        public const string STORAGE_PROVIDER = PREFIX + "STORAGE_PROVIDER";

        public const string STREAM_PROVIDER = PREFIX + "STREAM_PROVIDER";

        public const string CLIENT_MESSAGE_STREAM_NAMESPACE = PREFIX + "CLIENT_MESSAGE_STREAM";

        public const string HUB_MESSAGE_STREAM_NAMESPACE = PREFIX + "HUB_MESSAGE_STREAM";
    }
}

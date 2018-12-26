using System;

namespace Orleans.Messaging.SignalR.Internal
{
    public static class InternalSignalRConstants
    {
        public const string PREFIX = "ORLEANS_MESSAGING_SIGNALR_";

        public const string STORAGE_PROVIDER = PREFIX + "STORAGE_PROVIDER";

        public const string SEND_CLIENT_MESSAGE_STREAM_NAMESPACE = PREFIX + "SEND_CLIENT_MESSAGE_STREAM";

        public const string SEND_All_MESSAGE_STREAM_NAMESPACE = PREFIX + "SEND_ALL_MESSAGE_STREAM";

        public static readonly Guid DISCONNECTION_STREAM_ID = Guid.Parse("888AAEAD-7A35-4017-8302-B7C520C3D107");
    }
}

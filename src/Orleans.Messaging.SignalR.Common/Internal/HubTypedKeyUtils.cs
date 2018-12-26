using System;

namespace Orleans.Messaging.SignalR.Internal
{
    public class HubTypedKeyUtils
    {
        public const char SEPARATOR = ':';

        public static string ToHubTypedKeyString(string id, Guid hubTypeId)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return $"{id}{SEPARATOR}{hubTypeId}";
        }
    }
}

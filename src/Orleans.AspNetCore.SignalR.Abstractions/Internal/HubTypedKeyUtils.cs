using System;

namespace AspNetCore.SignalR.Orleans.Internal
{
    public class HubTypedKeyUtils
    {
        public const char SEPARATOR = ':';

        public static string ToHubTypedKeyString(string id, Guid hubTypeId)
        {
            return $"{id}{SEPARATOR}{hubTypeId}";
        }
    }

}

using System;

namespace Orleans.Messaging.SignalR.Internal
{
    internal static partial class GrainWithHubTypedKeyExtensions
    {
        public static Guid GetHubTypeId(this IGrainWithHubTypedStringKey grain)
        {
            return Guid.Parse(grain.GetPrimaryKeyString().Split(HubTypedKeyUtils.SEPARATOR)[1]);
        }

        public static string GetId(this IGrainWithHubTypedStringKey grain)
        {
            return grain.GetPrimaryKeyString().Split(HubTypedKeyUtils.SEPARATOR)[0];
        }
    }
}

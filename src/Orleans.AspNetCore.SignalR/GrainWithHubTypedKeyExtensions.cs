using Orleans;
using System;

namespace AspNetCore.SignalR.Orleans.Internal
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

        public static Guid GetHubTypeId(this IGrainWithHubTypedGuidKey grain)
        {
            var primaryKey = grain.GetPrimaryKey(out var keyExt);
            return Guid.Parse(keyExt);
        }

        public static Guid GetId(this IGrainWithHubTypedGuidKey grain)
        {
            return grain.GetPrimaryKey(out _);
        }
    }
}

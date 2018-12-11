using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AspNetCore.SignalR.Orleans
{
    public class DefaultClientSetPartitioner<TGrainInterface> : IClientSetPartitioner<TGrainInterface>
        where TGrainInterface : IClientSetPartitionGrain
    {
        private readonly IList<string> _clientSetManagerIds;

        public DefaultClientSetPartitioner()
        {
            _clientSetManagerIds = Enumerable.Range(0, 127).Select(i => $"{typeof(TGrainInterface).Name}_{i}").ToList();
        }

        public string GetPartitionId(string clientSetId, Guid hubTypeId)
        {
            using (var alg = SHA1.Create())
            {
                return GetElement(alg, _clientSetManagerIds, clientSetId);
            }
        }

        public IEnumerable<string> GetPartitionIds(Guid hubTypeId)
        {
            return _clientSetManagerIds;
        }

        private T GetElement<T>(HashAlgorithm alg, IList<T> list, string s)
        {
            var hash = alg.ComputeHash(Encoding.UTF8.GetBytes(s));
            var value = BitConverter.ToInt32(hash, 0);
            var index = Math.Abs(value) % list.Count;
            return list[index];
        }
    }
}

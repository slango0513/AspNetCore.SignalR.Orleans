using Orleans;
using Orleans.Messaging.SignalR;
using Sample.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sample.OrleansApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.ReadLine();
        }
    }

    public interface ISampleGrain : IGrainWithStringKey
    {
    }

    public class SampleGrainState
    {
        public ISet<string> Members { get; set; } = new HashSet<string>();
    }

    public class SampleGrain : Grain<SampleGrainState>, ISampleGrain
    {
        private IHubProxy _hubProxy;
        private IHubProxy _anotherHubProxy;

        public override Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider(SignalRConstants.STREAM_PROVIDER);
            _hubProxy = GrainFactory.GetHubProxy(streamProvider, Guid.Parse(HubTypeIds.SampleHub));
            _anotherHubProxy = GrainFactory.GetHubProxy(streamProvider, Guid.Parse(HubTypeIds.AnotherSampleHub));
            return base.OnActivateAsync();
        }

        public async Task AddToGroupWithConditionAsync(string connectionId, string groupName)
        {
            if (State.Members.Count <= 4)
            {
                await _hubProxy.AddToGroupAsync(connectionId, groupName);
                State.Members.Add(connectionId);
            }
        }

        public Task NotifyAllExceptCurrentMembersAsync()
        {
            return _hubProxy.SendAllExceptAsync("OnReceived", new object[] { "Hello, Client!" }, State.Members.ToList());
        }

        public Task NotifyUserFromAnotherHubAsync(string userId)
        {
            return _anotherHubProxy.SendUserAsync(userId, "OnReceived", new object[] { "Hello, User!" });
        }
    }
}

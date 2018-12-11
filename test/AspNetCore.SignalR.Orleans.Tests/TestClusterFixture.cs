using Orleans.TestingHost;
using System;

namespace AspNetCore.SignalR.Orleans.Tests
{
    public class TestClusterFixture : IDisposable
    {
        public TestCluster TestCluster { get; }

        public TestClusterFixture()
        {
            var builder = new TestClusterBuilder(3);
            builder.Options.ServiceId = Guid.NewGuid().ToString();
            builder.AddSiloBuilderConfigurator<TestSiloBuilderConfigurator>();
            builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
            var testCluster = builder.Build();

            testCluster.Deploy();

            TestCluster = testCluster;
        }

        public void Dispose()
        {
            TestCluster.Client.Close().Wait();
            TestCluster.StopAllSilos();
        }
    }
}

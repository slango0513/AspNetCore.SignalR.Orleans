using Orleans.Hosting;
using Orleans.TestingHost;

namespace AspNetCore.SignalR.Orleans.Tests
{
    public class TestSiloBuilderConfigurator : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder.AddSignalR(options =>
            {
            });
        }
    }
}

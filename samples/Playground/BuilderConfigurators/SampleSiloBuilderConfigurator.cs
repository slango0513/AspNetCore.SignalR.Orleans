using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace AspNetCore.SignalR.Orleans.Samples
{
    public class SampleSiloBuilderConfigurator : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder.AddSignalR(options =>
            {
            })
            .ConfigureLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Warning);
                builder.AddFilter("AspNetCore.SignalR.Orleans", LogLevel.Trace);
                builder.AddConsole();
            });
        }
    }
}

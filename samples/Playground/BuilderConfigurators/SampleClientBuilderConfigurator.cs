using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace AspNetCore.SignalR.Orleans.Samples
{
    public class SampleClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.AddSignalR(options => { })
                .ConfigureLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Warning);
                    builder.AddFilter("AspNetCore.SignalR.Orleans", LogLevel.Trace);
                    builder.AddConsole();
                });
        }
    }
}

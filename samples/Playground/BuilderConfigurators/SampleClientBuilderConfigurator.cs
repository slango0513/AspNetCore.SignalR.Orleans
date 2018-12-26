using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace Playground
{
    public class SampleClientBuilderConfigurator : IClientBuilderConfigurator
    {
        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.UseSignalR()
                .ConfigureLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Warning);
                    builder.AddFilter("AspNetCore.SignalR.Orleans", LogLevel.Trace);
                    builder.AddConsole();
                });
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.TestingHost;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Playground
{
    partial class Program
    {
        public static TestCluster TestCluster { get; private set; }

        static async Task Main(string[] args)
        {
            TestCluster = BuildTestCluster();
            await TestCluster.DeployAsync();
            //await BuildWebHost().RunAsync();
            await BuildHost(args).RunAsync();
        }

        public static IHost BuildHost(string[] args)
        {
            return new HostBuilder()
                .UseEnvironment(EnvironmentName.Development)
                .ConfigureHostConfiguration(builder =>
                {
                })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.SetBasePath(Directory.GetCurrentDirectory());
                    builder.AddJsonFile("appsettings.json");
                    builder.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", true);
                    builder.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(TestCluster);

                    services.AddHostedService<LineReadingService>();

                    services.AddLogging(builder =>
                    {
                        builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                        builder.AddConsole();
                    });
                })
                .Build();
        }

        public static TestCluster BuildTestCluster()
        {
            var builder = new TestClusterBuilder(3);
            builder.Options.ServiceId = Guid.NewGuid().ToString();
            builder.AddSiloBuilderConfigurator<SampleSiloBuilderConfigurator>();
            builder.AddClientBuilderConfigurator<SampleClientBuilderConfigurator>();
            return builder.Build();
        }
    }
}

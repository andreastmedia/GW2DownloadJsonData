using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GW2DownloadJsonData.ConsoleApp
{
    class Program
    {
        /// <summary>
        /// We are using the Dependency Injection pattern.<br/>
        /// The <see cref="Program.Main(string[])"/>:
        /// <list type="bullet">
        /// <item>Builds the host with pre-configured defaults by calling <see cref="Host.CreateDefaultBuilder"/>.</item>
        /// <item>Calls <see cref="IHostBuilder.ConfigureAppConfiguration"/> to load configurations from appsettings.json files and environment variables.</item>
        /// <item>Calls <see cref="HostingHostBuilderExtensions.ConfigureLogging"/> to setup logging to console and debug window.</item>
        /// <item>Calls <see cref="IHostBuilder.ConfigureServices"/> to add services to the container.</item>
        /// </list>
        /// </summary>
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostBuilderContext, configuration) =>
                { 
                    configuration.SetBasePath(Directory.GetCurrentDirectory())
                                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                 .AddEnvironmentVariables()
                                 .Build();
                })
                .ConfigureLogging((hostBuilderContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostBuilderContext.Configuration.GetSection("Logging"));
                    logging.AddDebug();
                    logging.AddConsole();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient("GuildWars2", client =>
                    {
                        client.BaseAddress = new Uri("https://api.guildwars2.com/v2/items/");
                        client.Timeout = new TimeSpan(0, 0, 2);
                    });
                    
                    services.AddSingleton<DataDownloader>();
                    services.AddTransient<DataDownloaderFunctions>();
                })
                .Build();

            ILogger logger = host.Services.GetService<ILogger<Program>>();

            var svc = ActivatorUtilities.CreateInstance<DataDownloader>(host.Services);
            try
            {
                await svc.Run();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error: {ex}");
            }
        }
    }
}
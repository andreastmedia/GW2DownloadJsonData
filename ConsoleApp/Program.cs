using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GW2DownloadJsonData.ConsoleApp
{
    /// <summary>
    /// Uses the JsonMethods namespace that you can find at "https://github.com/andreastmedia/Json_Processing".
    /// </summary>
    class Program
    {
        /// <summary>
        /// The Main method of the console application.
        /// </summary>
        /// <remarks>
        /// Added Dependency Injection in order to use the <see cref= "System.Net.Http.IHttpClientFactory" />.<br/>
        /// Also added Logging and Settings (appsettings.json).
        /// </remarks>
        static async Task Main(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            
            var host = Host.CreateDefaultBuilder()
                .ConfigureLogging((logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(configuration);
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
                    
                    services.AddSingleton<NamesDownloader>();
                    services.AddTransient<DataManager>();
                })
                .Build();

            ILogger logger = host.Services.GetService<ILogger<Program>>();

            var svc = ActivatorUtilities.CreateInstance<NamesDownloader>(host.Services);
            try
            {
                await svc.Run();
            }
            catch (Exception ex)
            {
                logger.LogError("Error: " + ex);
            }
        }
    }
}
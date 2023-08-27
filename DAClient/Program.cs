using Autodesk.Forge.Core;
using Autodesk.Forge.DesignAutomation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace DAClient;

class Program
{
    class ConsoleHost : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
    public static async Task Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureHostConfiguration(builder =>
             {
                 builder.AddJsonFile("appsettings.json", false, true);

             })
            .ConfigureAppConfiguration(builder =>
            {
                // TODO1: you must supply your appsettings.user.json with the following content:
                //{
                //    "Forge": {
                //        "ClientId": "<your client Id>",
                //        "ClientSecret": "<your secret>"
                //    }
                //}
                builder.AddJsonFile("appsettings.user.json");
                // Next line means that you can use Forge__ClientId and Forge__ClientSecret environment variables
                builder.AddEnvironmentVariables();
                // Finally, allow the use of "legacy" FORGE_CLIENT_ID and FORGE_CLIENT_SECRET environment variables
                builder.AddForgeAlternativeEnvironmentVariables();
            })
            .ConfigureLogging((hostContext, logBuilder) => {
                logBuilder.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                logBuilder.AddConsole();
            })
            .ConfigureServices((hostContext, services) =>
            {  // add our no-op host (required by the HostBuilder)
                services.AddHostedService<ConsoleHost>();
               
                // our own app where all the real stuff happens
                services.AddSingleton<App>();
             
                // add and configure DESIGN AUTOMATION
                services.AddDesignAutomation(hostContext.Configuration);
                services.AddSingleton<ILogger>(log =>
               log.GetRequiredService<ILogger<App>>());
            })
            .UseConsoleLifetime()
            .Build();
        using (host)
        {
            await host.StartAsync();

            // Get a reference to our App and run it
            var app = host.Services.GetRequiredService<App>();          
            await app.RunAsync();
            await host.StopAsync();
        }
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Logging;
using System;
using System.Net;

namespace Simcode.IdentityServer
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                //Log.Information("Starting host...");

                var host = CreateHostBuilder(args).Build();

                IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
                CultureHelper.InitializeUICulture(configuration);

                host.Run();

                return 0;
            }
            catch //(Exception ex)
            {
                //Log.Fatal(ex, "Host terminated unexpectedly.");
                return 1;
            }            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(
                    builder =>
                        builder.ClearProviders()
                            .AddSszLogger()
                    )
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {                        
                        webBuilder.UseStartup<Startup>();
                    })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MainBackgroundService>();
                })              
                .UseWindowsService();
    }
}
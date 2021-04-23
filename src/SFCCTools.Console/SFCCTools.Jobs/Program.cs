using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using SFCCTools.Jobs;
using SFCCTools.OCAPI;

using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SFCCTools.Core.Configuration;

namespace SFCCTools.Jobs
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: ConsoleTheme.None, outputTemplate: "[{Timestamp:o} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            var host = CreateHost(args);
            await host.RunAsync();
        }

        private static IHost CreateHost(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;
                    services.AddDbContext<BIDatabaseContext>((options) =>
                    {
                        options.UseNpgsql(configuration.GetConnectionString("Default"));
                    });

                    var sfccConfig = configuration.GetSection("SFCC");
                    services.Configure<SFCCEnvironment>(sfccConfig);
                    
                    services.AddAccountManagerClients();
                    services.AddOCAPIRestClients();
                    
                    var orderProcessingConfig = configuration.GetSection("OrderProcessing");
                    services.Configure<OrderProcessingConfig>(orderProcessingConfig);
                    
                    services.AddScheduledJob<OrderProcessing>(TimeSpan.FromMinutes(2));
                });

            var host = hostBuilder.UseConsoleLifetime().Build();
            using (var scope = host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
                
                logger.LogInformation("Initializing application");
                
                logger.LogInformation("Migrating database");
                using var context = scope.ServiceProvider.GetService<BIDatabaseContext>();
                context.Database.Migrate();
            }
            return host;
        }
    }
}
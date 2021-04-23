using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFCCTools.AccountManager;
using SFCCTools.Core.Configuration;
using SFCCTools.WebDAV;

namespace SFCCTools.FunctionalTests.WebDAV
{
    public class SFCCEnvironmentFixture
    {
        public ServiceProvider ServiceProvider { get; private set; }

        // Provides a test fixture for a real SFCC environment using local configuration
        public SFCCEnvironmentFixture()
        {
            var services = new ServiceCollection();
            var dir = Directory.GetCurrentDirectory();
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("testdata/appsettings.json", optional: true, reloadOnChange: false)
                .AddDWJsonConfiguration(Path.Combine(Directory.GetCurrentDirectory(), "testdata/dw.json"))
                .AddEnvironmentVariables()
                .Build();

            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole(options =>
                {
                    options.DisableColors = true;
                    options.IncludeScopes = true;
                });
            });

            services.Configure<SFCCEnvironment>(configuration);

            services.AddAccountManagerClients();
            services.AddSingleton<IWebDAVClient, SFCCWebDAVClient>();

            services.AddOCAPIRestClients();

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
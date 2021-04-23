using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFCCTools.Core.Configuration;
using Microsoft.EntityFrameworkCore;
using SFCCTools.Jobs;

namespace SFCCTools.FunctionalTests
{
    public class DatabaseFixture
    {
        public ServiceProvider ServiceProvider { get; private set; }

        // Provides a test fixture for an SFCC environment using local configuration
        public DatabaseFixture()
        {
            var services = new ServiceCollection();
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile("testdata/appsettings.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            services.AddDbContext<BIDatabaseContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("Default"));
            });

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
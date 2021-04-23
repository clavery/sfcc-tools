using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFCCTools.Core.Configuration;

namespace SFCCTools.Jobs
{
    public class TestJob : IJob
    {
        private ILogger<TestJob> _logger;

        public TestJob(ILogger<TestJob> logger, IOptions<SFCCEnvironment> envOptions)
        {
            envOptions.Value.IsValidEnvironment(true);
            _logger = logger;
        }
        public async Task Run(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Running test job");
        }
    }
}
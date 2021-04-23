using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SFCCTools.Jobs
{
    /// <summary>
    /// Implements a cron-like Scheduled Task as a Hosted Service (via IJob)
    ///
    /// Implement a concrete version of this class and use the ServiceCollection
    /// extensions (AddScheduledJob)
    ///
    /// Runs at 10 second granularity
    /// </summary>
    public class ScheduledJobRunner<T> : IHostedService where T : class, IJob
    {
        public TimeSpan Schedule;
        public bool RunImmediate = false;
        private readonly T _job;
        private readonly ILogger<ScheduledJobRunner<T>> _logger;

        private readonly CancellationTokenSource _stoppingCts =
            new CancellationTokenSource();

        private Timer _timer;
        private Task _executingTask;

        public ScheduledJobRunner(T job, ILogger<ScheduledJobRunner<T>> logger)
        {
            _job = job;
            Schedule = TimeSpan.FromMinutes(1);
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Scheduled job {JobType} started at interval {Schedule}", typeof(T),
                Schedule);
            _executingTask = RunJob(_stoppingCts.Token);

            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            return Task.CompletedTask;
        }

        private async Task RunJob(CancellationToken cancellationToken)
        {
            var nextRunTime = RunImmediate ? DateTime.Now : DateTime.Now.Add(Schedule);
            while (true && !cancellationToken.IsCancellationRequested)
            {
                if (DateTime.Now.CompareTo(nextRunTime) > 0)
                {
                    _logger.LogInformation("Executing Job Callback");
                    try
                    {
                        var started = DateTime.Now;
                        await _job.Run(cancellationToken);
                        var ended = DateTime.Now;
                        var duration = ended.Subtract(started).TotalSeconds;
                        _logger.LogInformation("Finished job {JobType} callback in {Duration}s", typeof(T), duration);
                        if (duration > Schedule.TotalSeconds)
                        {
                            _logger.LogWarning(
                                "Job {JobType} callback took longer than the schedule; Consider increasing the schedule duration",
                                typeof(T));
                        }
                    }
                    // TODO: use Polly to throttle on exceptions, retry and fail outright
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Job {JobType} callback faulted with unhandled exception",
                            typeof(T));
                    }

                    // TODO this should be consistent and not calculated after job run
                    nextRunTime = DateTime.Now.Add(Schedule);
                }

                await Task.Delay(10000, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Job {JobType} stopping.", typeof(T));
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            _timer?.Change(Timeout.Infinite, 0);

            try
            {
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, stoppingToken));
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _stoppingCts.Cancel();
        }
    }
}
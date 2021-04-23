using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFCCTools.Jobs;

public static class JobRunnerServiceExtensions
{
    public static IServiceCollection AddScheduledJob<T>(this IServiceCollection services, TimeSpan schedule)
        where T : class, IJob
    {
        services.AddTransient<T>();
        services.AddHostedService<ScheduledJobRunner<T>>(provider =>
            new ScheduledJobRunner<T>(provider.GetService<T>(), provider.GetService<ILogger<ScheduledJobRunner<T>>>())
            {
                Schedule = schedule,
                RunImmediate = true
            });
        return services;
    }
}
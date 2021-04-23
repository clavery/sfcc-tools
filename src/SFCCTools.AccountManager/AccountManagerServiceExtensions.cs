using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using SFCCTools.AccountManager;
using SFCCTools.Core.HTTP;

/// Adds ServiceCollection extension methods for configuring Account Manager
/// Authentication and Authenticated HTTP Client Factories with Retry Policies
public static class AccountManagerServiceExtensions
{
    static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                retryAttempt)));
    }

    public static IServiceCollection AddAccountManagerClients(this IServiceCollection services)
    {
        services.AddHttpClient("AccountManager", c =>
        {
            c.BaseAddress = new Uri("https://account.demandware.com/");
            c.DefaultRequestHeaders.Add("User-Agent", "SFCC-Tools");
        })
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient("BusinessManagerGrant", c => { c.DefaultRequestHeaders.Add("User-Agent", "SFCC-Tools"); })
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .AddPolicyHandler(GetRetryPolicy());

        // TODO: this really should be scoped to handle certain use cases (like web clients with unique reqs)
        services.AddSingleton<IAccountManagerAccessTokenAccessor, AccountManagerAccessTokenAccessor>();
        services.AddSingleton<IBusinessManagerAccessTokenAccessor, BusinessManagerAccessTokenAccessor>();

        services.AddTransient<TokenAuthenticatingHandler<IAccountManagerAccessTokenAccessor>>();
        services.AddTransient<TokenAuthenticatingHandler<IBusinessManagerAccessTokenAccessor>>();

        services.AddHttpClient("ClientCredentialsGrant")
#if DEBUG
            .AddHttpMessageHandler<HttpLoggingHandler>()
#endif
            .AddHttpMessageHandler<TokenAuthenticatingHandler<IAccountManagerAccessTokenAccessor>>();
        return services;
    }
}
using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Refit;
using SFCCTools.AccountManager;
using SFCCTools.Core.Configuration;
using SFCCTools.Core.HTTP;
using SFCCTools.OCAPI.DataAPI.Resources;
using SFCCTools.OCAPI.DataAPI.Types;
using SFCCTools.OCAPI.ShopAPI.Resources;

/// <summary>
/// Adds ServiceCollection Methods for OCAPI Rest clients built with Refit
/// </summary>
public static partial class OCAPIServiceExtensions
{
    /// <summary>
    /// Fixes an issue with dotnet core 3.1.1 and below that causes issues with refit
    /// Can be removed in 3.1.2
    /// </summary>
    public static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder,
        Func<HttpClient, TClient> factory)
        where TClient : class
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        // ReserveClient(builder, typeof(TClient), builder.Name);

        builder.Services.AddTransient<TClient>(s =>
        {
            var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(builder.Name);

            return factory(httpClient);
        });

        return builder;
    }

    public static IServiceCollection AddOCAPIRestClients(this IServiceCollection services)
    {
        // settings reflect OCAPI naming patterns and idioms
        var settings = new RefitSettings
        {
            ContentSerializer = new JsonContentSerializer(
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver()
                    {
                        // OCAPI uses snake case
                        NamingStrategy = new SnakeCaseNamingStrategy()
                    }
                }
            )
        };

        services.TryAddTransient<HttpLoggingHandler>();

        services
            .AddHttpClient("OCAPI_DataAPI", (provider, c) =>
            {
                var env = provider.GetRequiredService<IOptions<SFCCEnvironment>>().Value;
                c.BaseAddress = new Uri($"https://{env.Server}/s/-/dw/data/v20_8");
            })
            .AddHttpMessageHandler<TokenAuthenticatingHandler<IAccountManagerAccessTokenAccessor>>()
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .AddTypedClient<ISites>(c => Refit.RestService.For<ISites>(c, settings))
            .AddTypedClient<ICodeVersions>(c => Refit.RestService.For<ICodeVersions>(c, settings))
            .AddTypedClient<IJobs>(c => Refit.RestService.For<IJobs>(c, settings))
            .AddTypedClient<IJobExecutionSearch>(c => Refit.RestService.For<IJobExecutionSearch>(c, settings))
            .AddTypedClient<IGlobalPreferences>(c => Refit.RestService.For<IGlobalPreferences>(c, settings));

        services
            .AddHttpClient("OCAPI_ShopAPI",
                (provider, c) =>
                {
                    var env = provider.GetRequiredService<IOptions<SFCCEnvironment>>().Value;
                    if (!string.IsNullOrEmpty(env.SiteID))
                    {
                        c.BaseAddress = new Uri($"https://{env.Server}/s/{env.SiteID}/dw/shop/v20_8");
                    }
                    else
                    {
                        throw new SFCCEnvironmentException("A valid SiteID is required for the shop API");
                    }
                })
            .AddHttpMessageHandler<TokenAuthenticatingHandler<IAccountManagerAccessTokenAccessor>>()
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .AddTypedClient<IOrderSearch>(c => Refit.RestService.For<IOrderSearch>(c, settings));

        // These clients require a valid Business manager user
        // hence the use of the IBusinessManagerAccessTokenAccessor to use business manager grants
        services
            .AddHttpClient("OCAPI_ShopAPI_Agent",
                (provider, c) =>
                {
                    var env = provider.GetRequiredService<IOptions<SFCCEnvironment>>().Value;
                    c.BaseAddress = new Uri($"https://{env.Server}/s/{env.SiteID}/dw/shop/v20_2");
                })
            .AddHttpMessageHandler<TokenAuthenticatingHandler<IBusinessManagerAccessTokenAccessor>>()
            .AddHttpMessageHandler<HttpLoggingHandler>()
            .AddTypedClient<IOrders>(c => Refit.RestService.For<IOrders>(c, settings));
        return services;
    }
}
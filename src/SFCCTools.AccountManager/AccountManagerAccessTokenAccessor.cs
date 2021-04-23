using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFCCTools.Core.Configuration;

namespace SFCCTools.AccountManager
{
    public class AccountManagerAccessTokenAccessor : IAccountManagerAccessTokenAccessor
    {
        private readonly SFCCEnvironment _environment;
        private readonly IHttpClientFactory _clientFactory;
        private ILogger<AccountManagerAccessTokenAccessor> _logger;
        
        public AccessToken Token { get; private set; }

        public AccountManagerAccessTokenAccessor(IOptions<SFCCEnvironment> envOptions, IHttpClientFactory clientFactory,
            ILogger<AccountManagerAccessTokenAccessor> logger)
        {
            _environment = envOptions.Value;
            _environment.IsValidEnvironment(throwOnInvalid: true);
            _environment.HasClientCredentials(throwOnInvalid: true);
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public bool ValidateAccessToken()
        {
            return Token != null && !Token.IsExpired();
        }

        public async Task RenewAccessTokenAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "dwsso/oauth2/access_token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                })
            };

            var client = _clientFactory.CreateClient("AccountManager");
            var basicHeader = Encoding.ASCII.GetBytes($"{_environment.ClientID}:{_environment.ClientSecret}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(basicHeader));

            _logger.LogDebug("Sending access token request for {ClientID}", _environment.ClientID);
            var result = await client.SendAsync(request);

            if (!result.IsSuccessStatusCode)
            {
                _logger.LogError("Got response {StatusCode}", result.StatusCode);
                throw new Exception("Bad ClientID or secret");
            }

            await using var responseStream = await result.Content.ReadAsStreamAsync();
            Token = await JsonSerializer.DeserializeAsync<AccessToken>(responseStream);
            Token.ClientId = _environment.ClientID;
            _logger.LogDebug("Got access token {AccessToken}; expiring at {Expiration}", Token.Token,
                Token.Expiration);
        }
    }
}
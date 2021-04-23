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
    /// <summary>
    /// Gets OCAPI Access Tokens via a Business Manager Grant
    /// </summary>
    public class BusinessManagerAccessTokenAccessor : IBusinessManagerAccessTokenAccessor
    {
        private readonly SFCCEnvironment _environment;
        private readonly IHttpClientFactory _clientFactory;
        private ILogger<AccountManagerAccessTokenAccessor> _logger;

        public AccessToken Token { get; private set; }

        public BusinessManagerAccessTokenAccessor(IOptions<SFCCEnvironment> envOptions, IHttpClientFactory clientFactory, ILogger<AccountManagerAccessTokenAccessor> logger)
        {
            _environment = envOptions.Value;
            _environment.IsValidEnvironment(throwOnInvalid: true);
            _clientFactory = clientFactory;
            _logger = logger;

            if (string.IsNullOrEmpty(_environment.Username) || string.IsNullOrEmpty(_environment.ClientID))
            {
                throw new Exception("Business Manager token accessor requires both a valid business manager account and client ID/secret");
            }
        }

        public bool ValidateAccessToken()
        {
            return Token != null && !Token.IsExpired();
        }

        public async Task RenewAccessTokenAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_environment.Server}/dw/oauth2/access_token?client_id={_environment.ClientID}")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "urn:demandware:params:oauth:grant-type:client-id:dwsid:dwsecuretoken")
                })
            };

            var client = _clientFactory.CreateClient("BusinessManager");
            var basicHeader = Encoding.ASCII.GetBytes($"{_environment.Username}:{_environment.Password}:{_environment.ClientSecret}");
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
            _logger.LogDebug("Got access token {AccessToken}; expiring at {Expiration}", Token.Token, Token.Expiration);
        }
    }
}
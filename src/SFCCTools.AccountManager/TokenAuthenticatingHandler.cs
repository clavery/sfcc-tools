using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace SFCCTools.AccountManager
{
    /// <summary>
    /// Performs access token authentication for the HTTPClient using the given
    /// IAccessTokenAccessor (i.e. account manager, business manager grant, etc)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TokenAuthenticatingHandler<T> : DelegatingHandler where T : IAccessTokenAccessor
    {
        private readonly AsyncRetryPolicy<HttpResponseMessage> _policy;
        private T _accessTokenAccessor;
        private ILogger<TokenAuthenticatingHandler<T>> _logger;

        public TokenAuthenticatingHandler(T accessTokenAccessor, ILogger<TokenAuthenticatingHandler<T>> logger)
        {
            _logger = logger;
            _accessTokenAccessor = accessTokenAccessor;
            _policy = Policy.Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
                .RetryAsync(1, async (response, attempt) =>
                {
                    _logger.LogDebug("Retry handler executed; attempt {Attempt}", attempt);
                    await AuthenticateAsync();
                });
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Request an access token if we don't have one yet or if it has expired.
            if (!_accessTokenAccessor.ValidateAccessToken())
            {
                _logger.LogDebug("No valid access token; Authenticating");
                await AuthenticateAsync();
            }

            // Try to perform the request, re-authenticating gracefully if the call fails due to an expired or revoked access token.
            var result = await _policy.ExecuteAndCaptureAsync(() =>
            {
                request.Headers.Authorization = new AuthenticationHeaderValue(_accessTokenAccessor.Token.TokenType,
                    _accessTokenAccessor.Token.Token);

                if (!request.Headers.Contains("x-dw-client-id"))
                {
                    request.Headers.Add("x-dw-client-id", _accessTokenAccessor.Token.ClientId);
                }

                return base.SendAsync(request, cancellationToken);
            });

            return result.Outcome == OutcomeType.Failure ? result.FinalHandledResult : result.Result;
        }

        private async Task AuthenticateAsync()
        {
            await _accessTokenAccessor.RenewAccessTokenAsync();
        }
    }
}
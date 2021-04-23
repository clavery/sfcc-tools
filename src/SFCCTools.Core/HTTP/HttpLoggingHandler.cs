using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SFCCTools.Core.HTTP
{
    /// <summary>
    /// Logs the request and response bodies to Trace if they look
    /// like textual content
    ///
    /// HttpClients default trace logging only logs headers so this adds
    /// additional information
    /// </summary>
    public class HttpLoggingHandler : DelegatingHandler
    {
        readonly string[] types = new[] {"html", "text", "xml", "json", "txt", "x-www-form-urlencoded"};
        private ILogger<HttpLoggingHandler> _logger;

        public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger) : base()
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var req = request;

            var messages = new List<string>();


            if (req.Content != null)
            {
                if (req.Content is StringContent || this.IsTextBasedContentType(req.Headers) ||
                    this.IsTextBasedContentType(req.Content.Headers))
                {
                    var result = await req.Content.ReadAsStringAsync();

                    _logger.LogTrace("Request Body:\n{RequestBody}\n", result);
                }
            }

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var resp = response;
            if (resp.Content != null)
            {
                if (resp.Content is StringContent || this.IsTextBasedContentType(resp.Headers) ||
                    this.IsTextBasedContentType(resp.Content.Headers))
                {
                    var result = await resp.Content.ReadAsStringAsync();
                    _logger.LogTrace("Response Body:\n{RequestBody}\n", result);
                }
            }
            return response;
        }

        bool IsTextBasedContentType(HttpHeaders headers)
        {
            IEnumerable<string> values;
            if (!headers.TryGetValues("Content-Type", out values))
                return false;
            var header = string.Join(" ", values).ToLowerInvariant();

            return types.Any(t => header.Contains((string) t));
        }
    }
}
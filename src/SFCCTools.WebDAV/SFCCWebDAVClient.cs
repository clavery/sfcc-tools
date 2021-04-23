using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFCCTools.Core.Configuration;

namespace SFCCTools.WebDAV
{
    // NOTE: This class performs syncronous webDAV operations; Do not use it in an asp.net context without refactoring
    public class SFCCWebDAVClient : IWebDAVClient
    {
        private readonly ILogger _logger;
        private readonly SFCCEnvironment _env;
        private IHttpClientFactory _clientFactory;

        private readonly XNamespace DAV = "DAV:";

        public SFCCWebDAVClient(ILogger<SFCCWebDAVClient> logger, IOptions<SFCCEnvironment> envOptions,
            IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _env = envOptions.Value;
            _clientFactory = clientFactory;

            // web dav client requires a valid environment context
            _env.IsValidEnvironment(true);
        }

        private HttpClient GetClient()
        {
            HttpClient client;
            if (_env.HasClientCredentials())
            {
                // use client provided by account manager integration
                client = _clientFactory.CreateClient("ClientCredentialsGrant");
            }
            else
            {
                // Fallback behavior if no client credentials are available for the environment
                client = _clientFactory.CreateClient("BasicAuth");
                var byteArray = Encoding.ASCII.GetBytes($"{_env.Username}:{_env.Password}");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            }

            client.BaseAddress = new Uri($"https://{_env.Server}");
            return client;
        }

        private string getUrlForFilename(string location, string filename)
        {
            return $"/on/demandware.servlet/webdav/Sites/{location}/{filename}";
        }

        private WebDAVFile FileFromXML(XElement element)
        {
            var prop = element.Element(DAV + "propstat")?.Element(DAV + "prop");
            var file = new WebDAVFile
            {
                URI = element.Element(DAV + "href")?.Value,
                Exists = true,
                Contents = null,
                CreationDate = DateTime.Parse(prop?.Element(DAV + "creationdate")?.Value),
                LastModifiedDate = DateTime.Parse(prop?.Element(DAV + "getlastmodified")?.Value),
                Filename = prop?.Element(DAV + "displayname")?.Value,
                Length = prop?.Element(DAV + "getcontentlength") != null
                    ? Int32.Parse(prop?.Element(DAV + "getcontentlength")?.Value)
                    : 0,
                ContentType = prop?.Element(DAV + "getcontenttype")?.Value,
                ETag = prop?.Element(DAV + "getetag")?.Value
            };
            file.IsFile = prop?.Element(DAV + "resourcetype")?.Element(DAV + "collection") == null;
            return file;
        }

        public async Task<bool> DELETE(WebDAVLocation location, string filename)
        {
            _logger.LogDebug("Deleting file at {Location}/{Filename}", location, filename);
            var uri = getUrlForFilename(location.Value, filename);
            var result = await GetClient().DeleteAsync(uri);
            return result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Return a stream of the target file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task<Stream> GETStream(string path)
        {
            var client = GetClient();
            var result = await client.GetStreamAsync(path);
            return result;
        }

        public async Task<Stream> GETStream(WebDAVLocation location, string path)
        {
            var filePath = getUrlForFilename(location.Value, path);
            return await GETStream(filePath);
        }

        public async Task<WebDAVFile> GETInfo(string path)
        {
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://{_env.Server}{path}"),
                Method = new HttpMethod("PROPFIND")
            };
            var client = GetClient();
            var result = await client.SendAsync(request);
            if (result.IsSuccessStatusCode)
            {
                var bodyStream = await result.Content.ReadAsStreamAsync();
                var collection = await XElement.LoadAsync(bodyStream, LoadOptions.None, CancellationToken.None);
                var responses = collection.Descendants(DAV + "response");
                var file = responses.Select(FileFromXML).First();
                return file;
            }

            return null;
        }

        public async Task<WebDAVFile> GETInfo(WebDAVLocation location, string path)
        {
            var filePath = getUrlForFilename(location.Value, path);
            return await GETInfo(filePath);
        }

        public async Task<WebDAVFile> GET(string path)
        {
            var file = await GETInfo(path);
            var client = GetClient();
            if (file != null)
            {
                var result = await client.GetAsync(path);
                file.Contents = await result.Content.ReadAsByteArrayAsync();
                return file;
            }

            return null;
        }

        public async Task<WebDAVFile> GET(WebDAVLocation location, string filename)
        {
            _logger.LogDebug($"Getting file at {location}/{filename}");
            var uri = getUrlForFilename(location.Value, filename);
            return await GET(uri);
        }

        /// <summary>
        /// Update file contents if changed on server
        /// </summary>
        /// <param name="file"></param>
        /// <param name="useRange">Use http range header</param>
        /// <returns>true if the file was modified</returns>
        public async Task<bool> UpdateFile(WebDAVFile file, bool useRange = true)
        {
            if (!file.HasContents)
            {
                file.Contents = (await GET(file.URI)).Contents;
                return true;
            }
            else
            {
                var client = GetClient();
                var request = new HttpRequestMessage(HttpMethod.Get, file.URI);
                if (useRange)
                {
                    request.Headers.Range = new RangeHeaderValue(file.Length, null);
                }

                if (file.ETag != null)
                {
                    request.Headers.TryAddWithoutValidation("If-None-Match", file.ETag);
                }

                var result = await client.SendAsync(request);

                if (result.StatusCode == HttpStatusCode.NotModified ||
                    result.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    return false;
                }

                if (!result.IsSuccessStatusCode && result.StatusCode != HttpStatusCode.NotModified)
                {
                    throw new HttpRequestException("Response does not indicate success: " + result.StatusCode);
                }

                var body = await result.Content.ReadAsByteArrayAsync();
                if (body.Length == 0) return false;
                var newContents = new byte[body.Length + file.Contents.Length];
                Buffer.BlockCopy(file.Contents, 0, newContents, 0, file.Contents.Length);
                Buffer.BlockCopy(body, 0, newContents, file.Contents.Length, body.Length);
                file.Contents = newContents;
                file.Length = newContents.Length;
                return true;
            }
        }

        public async Task<IList<WebDAVFile>> ListDirectory(WebDAVLocation location, string directory)
        {
            _logger.LogDebug($"Listing Directory {location}/{directory}");
            var uri = getUrlForFilename(location.Value, directory);
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://{_env.Server}{uri}"),
                Method = new HttpMethod("PROPFIND")
            };
            var result = await GetClient().SendAsync(request);
            result.EnsureSuccessStatusCode();
            var bodyStream = await result.Content.ReadAsStreamAsync();
            var collection = await XElement.LoadAsync(bodyStream, LoadOptions.None, CancellationToken.None);
            var responses = collection.Descendants(DAV + "response");
            var files = responses.Select(FileFromXML);
            return files.Skip(1).ToList();
        }

        public async Task<bool> MakeDirectory(WebDAVLocation location, string directory)
        {
            _logger.LogDebug("Creating Directory {Location}/{Directory}", location, directory);
            var uri = getUrlForFilename(location.Value, directory);
            var file = await GET(location, directory);
            if (file != null)
            {
                _logger.LogDebug($"Directory {location}/{directory} exists");
                return true;
            }

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri($"https://{_env.Server}{uri}"),
                Method = new HttpMethod("MKCOL")
            };
            var result = await GetClient().SendAsync(request);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> PUT(WebDAVLocation location, string filename, string contents)
        {
            _logger.LogDebug("Putting file {Location}/{Filename}", location, filename);
            var uri = getUrlForFilename(location.Value, filename);
            var httpContent = new StringContent(contents);
            var result = await GetClient().PutAsync(uri, httpContent);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> PUT(WebDAVLocation location, string filename, byte[] contents)
        {
            _logger.LogDebug("Putting file {Location}/{Filename}", location, filename);
            var uri = getUrlForFilename(location.Value, filename);
            var httpContent = new ByteArrayContent(contents);
            var result = await GetClient().PutAsync(uri, httpContent);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> PUT(WebDAVLocation location, string filename, Stream contents,
            TransferProgressReporter.ReportProgressHandler progressHandler = null)
        {
            _logger.LogDebug("Putting file {Location}/{Filename}", location, filename);
            var uri = getUrlForFilename(location.Value, filename);
            var reporter = new TransferProgressReporter();
            if (progressHandler != null)
            {
                reporter.Handler += progressHandler;
            }

            var httpContent = new ProgressReportingHttpContent(contents, reporter);
            var result = await GetClient().PutAsync(uri, httpContent).ConfigureAwait(false);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> UNZIP(WebDAVLocation location, string filename)
        {
            _logger.LogDebug("Unzipping file at {Location}/{Filename}", location, filename);
            var uri = getUrlForFilename(location.Value, filename);
            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("method", "UNZIP")
                })
            };
            var result = await GetClient().SendAsync(request);
            return result.IsSuccessStatusCode;
        }
    }
}
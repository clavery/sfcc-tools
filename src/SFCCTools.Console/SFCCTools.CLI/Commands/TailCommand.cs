using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SFCCTools.WebDAV;

namespace SFCCTools.CLI.Commands
{
    public class TailCommand
    {
        private readonly ILogger _logger;
        private readonly IWebDAVClient _client;

        public static List<string> DefaultFilters = new List<string>()
        {
            "customerror",
            "customfatal",
            "fatal",
            "error",
            "api-deprecation"
        };

        private IConsoleOutput _console;

        public TailCommand(ILogger<TailCommand> logger, IConsoleOutput console, IWebDAVClient client)
        {
            _logger = logger;
            _client = client;
            _console = console;
        }

        public async Task<int> RunCommand(CancellationToken cancellationToken, List<string> filters,
            int interval = 3000, bool tailAll = false)
        {
            if (filters.Count == 0)
            {
                filters = DefaultFilters;
            }

            var filteredFiles = new List<WebDAVFile>();

            try
            {
                var contents = await _client.ListDirectory(WebDAVLocation.Logs, "");
                _logger.LogDebug("Got {NumFiles} files from logs", contents.Count);

                foreach (var filter in filters)
                {
                    var filteredContents = contents.Where(f => f.Filename.StartsWith(filter))
                        .OrderByDescending(f => f.LastModifiedDate);

                    if (!tailAll)
                    {
                        // only take the most recently modified
                        filteredContents = filteredContents.Take(1).OrderByDescending(f => f.LastModifiedDate);
                    }

                    filteredFiles.AddRange(filteredContents);
                }
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Cannot list logs; Check credentials");
                return 1;
            }

            if (filteredFiles.Count == 0)
            {
                _logger.LogWarning("No files found to tail...");
                return 1;
            }

            _logger.LogDebug("Filtered {NumFiles} from logs", filteredFiles.Count);
            await Task.WhenAll(filteredFiles.Select(f => _client.UpdateFile(f)));
            foreach (var file in filteredFiles)
            {
                OutputLastLogs(file, onlyLast: true);
            }

            try
            {
                while (true)
                {
                    await Task.Delay(interval, cancellationToken);
                    
                    // Check all files async, returning an anon object with the update status and last offset
                    var filesWithStatus = await Task.WhenAll(filteredFiles.Select(f =>
                    {
                        var offset = f.Contents.Length;
                        return Task.Run(async () =>
                        {
                            var updated = await _client.UpdateFile(f);
                            return new
                            {
                                File = f,
                                Updated = updated,
                                Offset = offset
                            };
                        }, cancellationToken);
                    }));

                    foreach (var f in filesWithStatus)
                    {
                        _logger.LogDebug("{File} update status: {UpdateStatus}", f.File.Filename, f.Updated);
                        if (f.Updated)
                        {
                            OutputLastLogs(f.File, onlyLast: false, offset: f.Offset);
                        }
                        
                    }
                }
            }
            catch (TaskCanceledException e)
            {
                return 0;
            }
        }

        /// <summary>
        /// Parse and output logs (or only the last for initial call) to stdout
        /// offsetting within the file contents
        /// </summary>
        /// <param name="file"></param>
        /// <param name="onlyLast"></param>
        /// <param name="offset"></param>
        private void OutputLastLogs(WebDAVFile file, bool onlyLast = false, int offset=0)
        {
            _console.Yellow($"------- {file.Filename}");
            var _logLines = Regex.Split(file.ContentsAsString(offset), @"^(?=\[\d{4}.+?\w{3}\])", RegexOptions.Multiline);
            var logLines = _logLines.Where(l => !String.IsNullOrWhiteSpace(l)).Select(l => l.Trim());
            if (onlyLast)
            {
                logLines = logLines.TakeLast(1);
            }
            foreach (var logLine in logLines)
            {
                _console.WriteLine(logLine);
            }

            _console.Yellow($"--------------------");
            _console.WriteLine("");
        }
    }
}
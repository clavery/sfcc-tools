using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFCCTools.Core.Cartridges;
using SFCCTools.Core.Configuration;
using SFCCTools.OCAPI.DataAPI.Resources;
using SFCCTools.WebDAV;

namespace SFCCTools.CLI.Commands
{
    public class WatchCommand
    {
        private readonly ILogger<WatchCommand> _logger;
        private readonly IConsoleOutput _console;
        private readonly IWebDAVClient _client;
        private readonly ICodeVersions _codeVersionsClient;
        private readonly SFCCEnvironment _env;

        public WatchCommand(ILogger<WatchCommand> logger, IConsoleOutput console, IWebDAVClient client,
            IOptions<SFCCEnvironment> env)
        {
            _logger = logger;
            _console = console;
            _client = client;
            _env = env.Value;

            if (string.IsNullOrEmpty(_env.CodeVersion))
            {
                throw new SFCCEnvironmentException("A valid code version must be specified in configuration");
            }
        }

        
        public async Task<int> RunCommand(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                location = Directory.GetCurrentDirectory();
            }

            var cartridges = CartridgeHelper.FindAllInDirectory(location);
            _logger.LogDebug("Found {NumCartridges} cartridges in {Location}", cartridges.Count, location);

            var watchers = new List<FileSystemWatcher>();
            foreach (var cartridge in cartridges)
            {
                _console.Write("Watching ");
                _console.Green(cartridge.Name, eol: "");
                _console.Write("...\n");
                var watcher = new FileSystemWatcher()
                {
                    Path = cartridge.Path,
                    NotifyFilter = NotifyFilters.LastWrite
                                   | NotifyFilters.FileName
                                   | NotifyFilters.DirectoryName
                };
                watcher.Changed += OnChanged;
                watcher.Renamed += OnRenamed;
                watcher.IncludeSubdirectories = true;
                watchers.Add(watcher);
                
                // TODO https://stackoverflow.com/questions/15519089/avoid-error-too-many-changes-at-once-in-directory/35432077#35432077
            }

            foreach (var watcher in watchers)
            {
                watcher.EnableRaisingEvents = true;
            }

            try
            {
                while (Console.Read() != null) ;
            }
            catch (TaskCanceledException e)
            {
                return 0;
            }

            return 0;
        }

        private void OnChanged(object source, FileSystemEventArgs e) =>
            // Specify what is done when a file is changed, created, or deleted.
            _console.WriteLine($"File: {e.FullPath} {e.ChangeType}");

        private void OnRenamed(object source, RenamedEventArgs e) =>
            // Specify what is done when a file is renamed.
            Console.WriteLine($"File: {e.OldFullPath} renamed to {e.FullPath}");
    }
}
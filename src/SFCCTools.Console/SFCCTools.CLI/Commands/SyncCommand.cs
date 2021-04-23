using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFCCTools.Core.Cartridges;
using SFCCTools.Core.Configuration;
using SFCCTools.OCAPI.DataAPI.Resources;
using SFCCTools.WebDAV;

namespace SFCCTools.CLI.Commands
{
    public class SyncCommand
    {
        private readonly ILogger<SyncCommand> _logger;
        private readonly IConsoleOutput _console;
        private readonly IWebDAVClient _client;
        private readonly ICodeVersions _codeVersionsClient;
        private readonly SFCCEnvironment _env;

        public SyncCommand(ILogger<SyncCommand> logger, IConsoleOutput console, IWebDAVClient client,
            ICodeVersions codeVersionsClient, IOptions<SFCCEnvironment> env)
        {
            _logger = logger;
            _console = console;
            _client = client;
            _codeVersionsClient = codeVersionsClient;
            _env = env.Value;

            if (string.IsNullOrEmpty(_env.CodeVersion))
            {
                throw new SFCCEnvironmentException("A valid code version must be specified in configuration");
            }
        }

        public async Task<int> RunCommand(string location, bool deleteAndReactivate = false)
        {
            if (string.IsNullOrEmpty(location))
            {
                location = Directory.GetCurrentDirectory();
            }

            var cartridges = CartridgeHelper.FindAllInDirectory(location);
            _logger.LogDebug("Found {NumCartridges} cartridges in {Location}", cartridges.Count, location);

            foreach (var cartridge in cartridges)
            {
                _console.Write("Collecting ");
                _console.Green(cartridge.Name, eol: "");
                _console.Write("...\n");
            }

            if (deleteAndReactivate)
            {
                _console.WriteLine($"Deleting code version {_env.CodeVersion}");
                if (!await _client.DELETE(WebDAVLocation.Cartridges, _env.CodeVersion))
                {
                    _console.Yellow("Code version was not deleted (may not exist)");
                }
            }

            await using (var ms = new MemoryStream())
            {
                _console.Write("Syncing code version ");
                _console.Yellow(_env.CodeVersion, eol: "");
                _console.Write(" on ");
                _console.Yellow(_env.Server);

                // Write a zip archive to the in memory stream
                CartridgeHelper.CartridgesToZipFile(cartridges, _env.CodeVersion, ms);
                
                var progressBar = _console.CreateProgressBar();

                if (!await _client.PUT(WebDAVLocation.Cartridges, $"{_env.CodeVersion}.zip", ms,
                    progressBar.ProgressHandler))
                {
                    _logger.LogError("Could not upload code version");
                    return 1;
                }
            }

            _console.WriteLine("Extracting...");
            if (!await _client.UNZIP(WebDAVLocation.Cartridges, $"{_env.CodeVersion}.zip"))
            {
                _logger.LogError("Could not unzip code version");
                return 1;
            }

            //await _client.DELETE(WebDAVLocation.Cartridges, $"{_env.CodeVersion}.zip");
            _console.Green($"Successfully synced cartridges with {_env.Server}");

            if (deleteAndReactivate)
            {
                _console.Write("Activating code version...");
                if (!await _codeVersionsClient.ActivateCodeVersion(_env.CodeVersion))
                {
                    _logger.LogError("Could not activate code version");
                    return 1;
                }
            }

            return 0;
        }
    }
}
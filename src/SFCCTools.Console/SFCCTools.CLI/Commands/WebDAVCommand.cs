using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFCCTools.Core.Configuration;
using SFCCTools.WebDAV;

namespace SFCCTools.CLI.Commands
{
    /// <summary>
    /// Interactive command for interfacing with an SFCC instance WebDAV
    /// including custom extensions (UNZIP, etc)
    /// </summary>
    public class WebDAVCommand
    {
        private readonly ILogger<TailCommand> _logger;
        private readonly IConsoleOutput _console;
        private readonly IWebDAVClient _client;
        private WebDAVLocation _location;
        private SFCCEnvironment _env;
        private CommandLineApplication _app;
        private string _localWorkingDirectory;
        private Uri _workingDirectory;

        public WebDAVCommand(ILogger<TailCommand> logger, IConsoleOutput console, IWebDAVClient client,
            IOptions<SFCCEnvironment> _envOption)
        {
            _env = _envOption.Value;
            _logger = logger;
            _console = console;
            _client = client;
            _location = WebDAVLocation.Logs;
            // use a dummy URI for managing location state
            _workingDirectory = new Uri("/", UriKind.Absolute);
            _localWorkingDirectory = Directory.GetCurrentDirectory();
        }

        public async Task<int> RunCommand(WebDAVLocation location, string directory = "")
        {
            if (!string.IsNullOrEmpty(location.Value))
            {
                _location = location;
            }

            if (!string.IsNullOrEmpty(directory))
            {
                directory = directory.EndsWith("/") ? directory : directory + "/";
                _workingDirectory = new Uri("/" + directory, UriKind.Absolute);
            }

            _app = new CommandLineApplication {Name = ""};

            _app.HelpOption(inherited: true);
            
            _app.Command("ls", LsCommandHandler);
            _app.Command("lcd", LcdCommandHandler);
            _app.Command("cd", CdCommandHandler);
            _app.Command("get", GetCommandHandler);
            
            while (true)
            {
                DisplayPrompt();
                var cmd = _console.ReadLine();
                if (cmd == null)
                    return 0;
                try
                {
                    var result = await _app.ExecuteAsync(cmd.Split(" "));
                }
                
                catch (CommandParsingException ex)
                {
                    _console.Red("Invalid command.");
                    continue;
                }
            }

            return 0;
        }

        private void LsCommandHandler(CommandLineApplication lsCmd)
        {
            lsCmd.Description = "List directory";
            var sortByTime = lsCmd.Option("-t", "Sort by modified time", CommandOptionType.NoValue);
            var filter = lsCmd.Argument("filter", "Regular expression to filter by");

            lsCmd.OnExecuteAsync(async token =>
            {
                var contents = await _client.ListDirectory(_location, _workingDirectory.AbsolutePath);

                if (!string.IsNullOrEmpty(filter.Value))
                {
                    var filterRegex = new Regex(filter.Value);
                    contents = contents.Where(f => filterRegex.IsMatch(f.Filename)).ToList();
                }
                
                contents = sortByTime.HasValue()
                    ? contents.OrderByDescending(f => f.LastModifiedDate).ToList()
                    : contents.OrderBy(f => f.Filename).ToList();

                foreach (var file in contents)
                {
                    _console.Write($"{file.LastModifiedDate:s}\t");
                    _console.Write($"{file.Length,10}\t");
                    if (file.IsDirectory)
                    {
                        _console.Blue($"{file.Filename}");
                    }
                    else
                    {
                        _console.WriteLine($"{file.Filename}");
                    }
                }

                return 0;
            });
        }
        
        private void GetCommandHandler(CommandLineApplication getCmd)
        {
            getCmd.Description = "Get a file";
            var filename = getCmd.Argument("filename", "filename to download");
            filename.IsRequired();

            getCmd.OnExecuteAsync(async token =>
            {
                var targetFile = new Uri(_workingDirectory, new Uri(filename.Value, UriKind.RelativeOrAbsolute));
                var file = await _client.GETInfo(_location, targetFile.AbsolutePath);
                if (file == null)
                {
                    _logger.LogError("File not found");
                    return 1;
                }

                if (file.IsDirectory)
                {
                    _logger.LogError("File is a directory");
                    return 1;
                }
                
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(_localWorkingDirectory, file.Filename)))
                {
                    _logger.LogDebug("Getting stream of file contents");
                    var stream = await _client.GETStream(_location, targetFile.AbsolutePath);
                    _logger.LogDebug("Copying stream to output file");
                    await stream.CopyToAsync(outputFile.BaseStream, token);
                }
                    
                return 0;
            });
        }
        private void CdCommandHandler(CommandLineApplication cdCmd)
        {
            cdCmd.Description = "Change directory";
            var directory = cdCmd.Argument("directory", "directory to change to");
            directory.IsRequired();

            cdCmd.OnExecuteAsync(async token =>
            {
                var _directory = directory.Value.EndsWith("/") ? directory.Value : directory.Value + "/";
                var targetDir = new Uri(_directory, UriKind.RelativeOrAbsolute);
                if (!targetDir.IsAbsoluteUri)
                {
                    targetDir = new Uri(_workingDirectory, targetDir);
                }
                _logger.LogInformation("Checking for valid directory {Directory}", targetDir.AbsolutePath);
                var dir = await _client.GET(_location, targetDir.AbsolutePath);

                if (dir != null && dir.IsDirectory)
                {
                    _workingDirectory = targetDir;
                }
                else
                {
                    _logger.LogError("Invalid directory: {Directory}", targetDir.AbsolutePath);
                    return 1;
                }
                return 0;
            });
        }
        
        private void LcdCommandHandler(CommandLineApplication lcdCommand)
        {
            lcdCommand.Description = "Change working directory";
            var newDir = lcdCommand.Argument("dir", "Directory to change to");
            newDir.IsRequired();

            lcdCommand.OnExecuteAsync(async token =>
            {
                try
                {
                    Directory.SetCurrentDirectory(newDir.Value.StartsWith('/')
                        ? newDir.Value
                        : Path.Join(Directory.GetCurrentDirectory(), newDir.Value));
                }
                catch (DirectoryNotFoundException ex)
                {
                    _logger.LogError(ex, "Directory not found.");
                    return 1;
                }

                _logger.LogInformation("Changed local directory to {NewDir}", Directory.GetCurrentDirectory());
                return 0;
            });
        }

        private void DisplayPrompt()
        {
            _console.Write("[");
            _console.Blue($"{_env.Server}", eol: "");
            _console.Write(":");
            _console.Yellow($"{_location.Value}", eol: "");
            _console.Write("]");
            _console.Write($" {_workingDirectory.AbsolutePath}> ");
        }
    }
}
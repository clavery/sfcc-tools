using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using SFCCTools.CLI.Commands;
using SFCCTools.Core.Configuration;
using SFCCTools.WebDAV;
using WebWindows;

namespace SFCCTools.CLI
{
    public class Program
    {
        private static CommandOption<LogEventLevel> _optionLogLevel;
        private static CommandOption _optionProject;
        private static CommandOption _optionEnvironment;
        private static CommandLineApplication _app;

        public static int Main(string[] args)
        {
            _app = new CommandLineApplication()
            {
                Name = "sfcc",
                Description = "SFCC Tools CLI",
                ExtendedHelpText = "SFCC Tools CLI is a command line application that allows for performing various\n" +
                                   "actions on an SFCC instance. See the subcommands for the available commands"
            };

            _app.HelpOption(inherited: true);
            _app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);

            _optionLogLevel = _app.Option<LogEventLevel>("--log <MinimumLevel>",
                "Set log level Verbose,Debug,Information,Warning,Error,Fatal (DEFAULT: Warning)",
                CommandOptionType.SingleOrNoValue);

            // Options to derive from global ~/.dwre.json file
            _optionProject = _app.Option("-p|--project <Project>", ".dwre.json Project Name",
                CommandOptionType.SingleValue);
            _optionEnvironment = _app.Option("-e|--env <Environment>", ".dwre.json Environment Name",
                CommandOptionType.SingleValue);

            // Standard Instance Related Options
            var optionServer =
                _app.Option("--server <Server>", "SFCC Server Hostname", CommandOptionType.SingleValue);
            var optionUsername = _app.Option("--username <Username>", "SFCC Username", CommandOptionType.SingleValue);
            var optionPassword = _app.Option("--password <Password>", "SFCC Password", CommandOptionType.SingleValue);
            var optionClientID =
                _app.Option("--clientid <ClientID>", "SFCC API Client ID", CommandOptionType.SingleValue);
            var optionClientSecret = _app.Option("--clientsecret <ClientSecret>", "SFCC API Client Secret",
                CommandOptionType.SingleValue);
            var optionCodeVersion = _app.Option("--codeversion <CodeVersion>", "Code version to interface with",
                CommandOptionType.SingleValue);

            _app.Command("tail", tailCmd =>
            {
                tailCmd.Description = "Follow and output log files on instance";
                var optionFilters = tailCmd.Option("-f|--filters <Filters>",
                    "Comma separated filters (default: error,customerror,fatal,customfatal,api-deprecation)",
                    CommandOptionType.MultipleValue);
                var optionAll = tailCmd.Option("-a|--all",
                    "Tail all matching logs not just the latest", CommandOptionType.NoValue);
                var optionInterval = tailCmd.Option<int>("-i|--interval",
                    "Polling interval in milliseconds (Default: 3000ms)", CommandOptionType.SingleValue);

                tailCmd.OnExecuteAsync(WithServiceProvider(async (CancellationToken, sp) =>
                {
                    var tailCommand = sp.GetService<TailCommand>();
                    return await tailCommand.RunCommand(CancellationToken, optionFilters.Values,
                        optionInterval.HasValue() ? optionInterval.ParsedValue : 3000, optionAll.HasValue());
                }));
            });

            _app.Command("webdav", webdavCmd =>
            {
                webdavCmd.Description = "Interactive WebDAV Client";
                var location = webdavCmd.Option("-l|--location",
                    "Location Impex,Logs,Cartridges,Securitylogs,Temp,Realmdata", CommandOptionType.SingleValue);
                var directory = webdavCmd.Argument("directory",
                    "Location Impex,Logs,Cartridges,Securitylogs,Temp,Realmdata");
                webdavCmd.OnExecuteAsync(WithServiceProvider(async (CancellationToken, sp) =>
                {
                    return await sp.GetService<WebDAVCommand>()
                        .RunCommand(new WebDAVLocation(location.Value()), directory.Value);
                }));
            });

            _app.Command("sync", syncCmd =>
            {
                syncCmd.Description = "Find and sync cartridges to instance";
                var deleteOption = syncCmd.Option<bool>("-d|--delete",
                    "Delete and reactivate code version", CommandOptionType.NoValue);
                var directory = syncCmd.Argument("directory",
                    "Target directory (Default: Current working directory");
                syncCmd.OnExecuteAsync(WithServiceProvider(async (CancellationToken, sp) =>
                {
                    return await sp.GetService<SyncCommand>()
                        .RunCommand(directory.Value, deleteOption.HasValue());
                }));
            });

            _app.Command("export", exportCmd =>
            {
                exportCmd.Description = "Export site import/export archive";
                exportCmd.OnExecuteAsync(WithServiceProvider(async (CancellationToken, sp) => { return 0; }));
            });

            _app.Command("watch", watchCmd =>
            {
                watchCmd.Description = "Watch for files and upload to code version";
                var directory = watchCmd.Argument("directory",
                    "Target directory (Default: Current working directory");

                watchCmd.OnExecuteAsync(WithServiceProvider(async (CancellationToken, sp) =>
                {
                    var watchCommand = sp.GetService<WatchCommand>();
                    return await watchCommand.RunCommand(directory.Value);
                }));
            });
            
            // Default command
            _app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                _app.ShowHelp();
                return 1;
            });

            var result = 0;
            try
            {
                result = _app.Execute(args);
            }
            catch (SFCCEnvironmentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                if (_optionLogLevel.HasValue()) throw;
                return 1;
            }
            catch (CommandParsingException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            return result;
        }

        /// <summary>
        /// Provides the async callback with a newly minted ServiceProvider configured from
        /// the current state of the program
        /// </summary>
        /// <param name="cb">Callback to execute when (sub)command is executed</param>
        /// <returns></returns>
        private static Func<CancellationToken, Task<int>> WithServiceProvider(
            Func<CancellationToken, IServiceProvider, Task<int>> cb)
        {
            return async (cancellationToken) =>
            {
                var serviceProvider = ConfigureServices();
                var result = await cb(cancellationToken, serviceProvider);
                serviceProvider.Dispose();
                return result;
            };
        }

        private static ServiceProvider ConfigureServices()
        {
            string homedir;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                homedir = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            }
            else
            {
                homedir = Environment.GetEnvironmentVariable("HOME");
            }

            // These optional CLI params are necessary for service configuration
            var project = _optionProject.Value();
            var environment = _optionEnvironment.Value();

            var services = new ServiceCollection();
            var configurationBuilder = new ConfigurationBuilder()
                .AddSecretsConfiguration()
                .AddDWREJsonConfiguration(Path.Combine(homedir, ".dwre.json"), project, environment);

            if (project == null && environment == null)
            {
                // only include dw.json if specific project/env is not given
                configurationBuilder.AddDWJsonConfiguration(Path.Combine(Directory.GetCurrentDirectory(), "dw.json"));
            }

            var configuration = configurationBuilder.AddJsonFile(
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                    optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("SFCC_")
                .AddCLIConfiguration(_app)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(_optionLogLevel.HasValue() ? _optionLogLevel.ParsedValue : LogEventLevel.Warning)
                .ReadFrom.Configuration(configuration)
                //{SourceContext:l} if we want context
                .WriteTo.Console(outputTemplate:"[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            services.AddLogging(logging => { logging.AddSerilog(); });

            services.AddSingleton<IConsoleOutput, ColorConsole>();

            services.Configure<SFCCEnvironment>(configuration);

            services.AddAccountManagerClients();
            services.AddSingleton<IWebDAVClient, SFCCWebDAVClient>();
            services.AddOCAPIRestClients();

            // Command Implementations
            services.AddSingleton<TailCommand>();
            services.AddSingleton<WebDAVCommand>();
            services.AddSingleton<SyncCommand>();
            services.AddSingleton<WatchCommand>();

            return services.BuildServiceProvider();
        }
    }
}
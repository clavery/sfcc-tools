using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace SFCCTools.CLI
{
    public class CLIConfigurationSource : IConfigurationSource
    {
        public CLIConfigurationSource(CommandLineApplication app)
        {
            App = app;
        }

        public CommandLineApplication App { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new CLIConfigurationProvider(this);
        }
    }

    public class CLIConfigurationProvider : ConfigurationProvider
    {
        private CLIConfigurationSource _source;

        public CLIConfigurationProvider(CLIConfigurationSource source)
        {
            _source = source;
        }

        public override void Load()
        {
            var data = new Dictionary<string, string>();

            foreach (var option in _source.App.Options)
            {
                if (option.Value() != null && option.ValueName != null) {
                    data.Add(option.ValueName, option.Value());
                }
            }
            this.Data = data;
        }
    }
}
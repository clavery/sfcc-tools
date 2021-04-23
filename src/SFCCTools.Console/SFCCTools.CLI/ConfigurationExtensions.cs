using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using SFCCTools.CLI;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddCLIConfiguration(
        this IConfigurationBuilder configuration, CommandLineApplication app)
    {
        configuration.Add(new CLIConfigurationSource(app));
        return configuration;
    }
}
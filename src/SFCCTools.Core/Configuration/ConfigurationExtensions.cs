using Microsoft.Extensions.Configuration;
using SFCCTools.Core.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddDWJsonConfiguration(
        this IConfigurationBuilder configuration, string path)
    {
        configuration.Add(new DWJSONConfigurationSource(path));
        return configuration;
    }

    public static IConfigurationBuilder AddDWREJsonConfiguration(
        this IConfigurationBuilder configuration, string path, string project, string environment)
    {
        configuration.Add(new DWREJSONConfigurationSource(path, project, environment));
        return configuration;
    }
    
    public static IConfigurationBuilder AddSecretsConfiguration(
        this IConfigurationBuilder configuration)
    {
        configuration.Add(new SecretsConfigurationSource());
        return configuration;
    }
}
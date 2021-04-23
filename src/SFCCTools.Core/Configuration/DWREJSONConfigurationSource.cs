using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace SFCCTools.Core.Configuration
{
    /// <summary>
    /// Necessary to use "dotfiles" from Home Directories
    /// </summary>
    public class DotFileFileProvider : PhysicalFileProvider
    {
        public DotFileFileProvider(string path) : base(path, ExclusionFilters.None)
        {
        }
    }

    /// <summary>
    /// Loads configuration from a ~/.dwre.json file which allows for organization
    /// of instances into projects and environments.
    ///
    /// Additionally configuration is sourced from the systems keychain (currently only on MacOS) to provide
    /// for secure storage of secrets using the following keys in priority order:
    ///
    /// - instance hostname
    /// - SFCCAccountManager
    /// - account.demandware.com
    /// </summary>
    public class DWREJSONConfigurationSource : JsonConfigurationSource
    {
        public DWREJSONConfigurationSource(string path, string project, string environment) : base()
        {
            this.Optional = true;
            this.Project = project;
            this.Environment = environment;
            this.ReloadOnChange = false;
            var directory = System.IO.Path.GetDirectoryName(path);
            var pathToFile = System.IO.Path.GetFileName(path);
            this.FileProvider = new DotFileFileProvider(directory);
            this.Path = pathToFile;
        }

        public string Project { get; }
        public string Environment { get; }

        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DWREJSONConfigurationProvider(this, Project, Environment);
        }
    }

    public class DWREJSONConfigurationProvider : JsonConfigurationProvider
    {
        public DWREJSONConfigurationProvider(DWREJSONConfigurationSource source, string project, string environment) :
            base(source)
        {
            Project = project;
            Environment = environment;
        }

        public string Project { get; }
        public string Environment { get; }

        public override void Load()
        {
            base.Load();

            string targetProject;
            if (!string.IsNullOrEmpty(Project))
            {
                targetProject = Project;
            }
            else
            {
                this.Data.TryGetValue("defaultProject", out targetProject);
            }

            if (!string.IsNullOrEmpty(targetProject))
            {
                string targetEnvironment;
                if (!string.IsNullOrEmpty(Environment))
                {
                    targetEnvironment = Environment;
                }
                else
                {
                    this.Data.TryGetValue($"projects:{targetProject}:defaultEnvironment", out targetEnvironment);
                }

                if (!string.IsNullOrEmpty(targetEnvironment))
                {
                    // attempt to load keys from the target environment

                    if (this.Data.TryGetValue($"projects:{targetProject}:environments:{targetEnvironment}:username",
                        out var username))
                    {
                        this.Data.Add("Username", username);
                    }

                    if (this.Data.TryGetValue($"projects:{targetProject}:environments:{targetEnvironment}:password",
                        out var password))
                    {
                        this.Data.Add("Password", password);
                    }

                    if (this.Data.TryGetValue($"projects:{targetProject}:environments:{targetEnvironment}:server",
                        out var server))
                    {
                        this.Data.Add("Server", server);
                    }

                    if (this.Data.TryGetValue($"projects:{targetProject}:environments:{targetEnvironment}:codeVersion",
                        out var codeVersion))
                    {
                        this.Data.Add("CodeVersion", codeVersion);
                    }

                    if (this.Data.TryGetValue($"projects:{targetProject}:environments:{targetEnvironment}:clientID",
                        out var clientID))
                    {
                        this.Data.Add("ClientID", clientID);
                    }

                    if (this.Data.TryGetValue($"projects:{targetProject}:environments:{targetEnvironment}:clientSecret",
                        out var clientSecret))
                    {
                        this.Data.Add("ClientSecret", clientSecret);
                    }

                    // Support account manager username at top of file if not specified
                    this.Data.TryGetValue("accountManager:username", out var accountManagerUsername);
                    if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(accountManagerUsername))
                    {
                        this.Data.Add("Username", accountManagerUsername);
                    }

                    this.Data.TryGetValue("accountManager:password", out var accountManagerPassword);
                    if (string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(accountManagerPassword))
                    {
                        this.Data.Add("Password", accountManagerPassword);
                    }

                    // Finally attempt to retrieve information from keychain if no password but username available
                    if (this.Data.ContainsKey("Username") && !this.Data.ContainsKey("Password") &&
                        !string.IsNullOrEmpty(server))
                    {
                        var uname = this.Data["Username"];
                        // check if we have a password for this exact server and username
                        var secretPair = SecretsConfigurationProvider.InternetPasswordFromKeychain(server, uname);

                        if (secretPair != null)
                        {
                            this.Data.Add("Password", secretPair.Password);
                        }
                    }
                }
            }
        }

    }
}
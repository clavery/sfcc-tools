#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace SFCCTools.Core.Configuration
{
    /// <summary>
    /// Providers username, password and client credentials from the systems keychain
    ///
    /// TODO: support platforms other than MacOS
    /// </summary>
    public class SecretsConfigurationSource : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SecretsConfigurationProvider();
        }
    }

    public class SecretsConfigurationProvider : ConfigurationProvider
    {
        public override void Load()
        {
            // use SFCCAccountManager and then account.demandware.com application password if set
            var secretPair = SecretsConfigurationProvider.ApplicationPasswordFromKeychain("SFCCAccountManager") ??
                             SecretsConfigurationProvider.InternetPasswordFromKeychain("account.demandware.com");
            
            if (secretPair != null)
            {
                this.Data.Add("Username", secretPair.Username);
                this.Data.Add("Password", secretPair.Password);
            }

            // allow storage of global client credentials in secret story
            secretPair =
                SecretsConfigurationProvider.ApplicationPasswordFromKeychain("SFCCAccountManagerClientCredentials");
            
            if (secretPair != null)
            {
                this.Data.Add("ClientID", secretPair.Username);
                this.Data.Add("ClientSecret", secretPair.Password);
            }
        }

        public class SecretPair
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public static SecretPair ApplicationPasswordFromKeychain(string appName)
        {
            return ApplicationPasswordFromKeychain(appName, null);
        }

        public static SecretPair ApplicationPasswordFromKeychain(string appName, string username)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/security",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        ArgumentList =
                        {
                            "find-generic-password",
                            "-g",
                            "-s",
                            appName
                        }
                    };
                    if (!string.IsNullOrEmpty(username))
                    {
                        startInfo.ArgumentList.Add("-a");
                        startInfo.ArgumentList.Add(username);
                    }

                    var process = new Process()
                    {
                        StartInfo = startInfo
                    };
                    process.Start();
                    string result = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return null;
                    }

                    return ParseSecurityOutput(result + "\n" + error);
                }
                catch (Exception e)
                {
                    return null;
                }
            }

            return null;
        }

        public static SecretPair InternetPasswordFromKeychain(string server)
        {
            return InternetPasswordFromKeychain(server, null);
        }

        public static SecretPair InternetPasswordFromKeychain(string server, string username)
        {
            // TODO: support other operating system keychain implementations
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/security",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        ArgumentList =
                        {
                            "find-internet-password",
                            "-g",
                            "-s",
                            server
                        }
                    };
                    if (!string.IsNullOrEmpty(username))
                    {
                        startInfo.ArgumentList.Add("-a");
                        startInfo.ArgumentList.Add(username);
                    }

                    var process = new Process()
                    {
                        StartInfo = startInfo
                    };
                    process.Start();
                    string result = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return null;
                    }

                    return ParseSecurityOutput(result + "\n" + error);
                }
                catch (Exception e)
                {
                    return null;
                }
            }

            return null;
        }

        public static SecretPair? ParseSecurityOutput(string output)
        {
            var accountRegex = new Regex(@"""acct""<blob>=""(.+)""", RegexOptions.Multiline);
            var passwordRegex = new Regex(@"password:\s""(.+)""", RegexOptions.Multiline);
            var userNameMatch = accountRegex.Match(output);
            var passwordMatch = passwordRegex.Match(output);
            if (!userNameMatch.Success || !passwordMatch.Success)
            {
                Console.WriteLine(userNameMatch.Success);
                Console.WriteLine(passwordMatch.Success);
                return null;
            }

            return new SecretPair()
            {
                Username = userNameMatch.Groups[1].ToString(),
                Password = passwordMatch.Groups[1].ToString()
            };
        }
    }
}
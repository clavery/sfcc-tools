using SFCCTools.Core.Configuration;
using Xunit;

namespace SFCCTools.UnitTests.Configuration
{
    public class TestSecretsConfiguration
    {
        [Fact]
        public void TestParsesMacOSSecurityCommandOutput()
        {
            var output = @"keychain: ""/Users/test/Library/Keychains/login.keychain-db""
version: 512
class: ""genp""
attributes:
    0x00000007 <blob>=""SFCCAccountManager""
    0x00000008 <blob>=<NULL>
    ""acct""<blob>=""test@example.com""
    ""cdat""<timedate>=0x32303230303230393136333230325A00  ""20200209163202Z\000""
    ""crtr""<uint32>=<NULL>
    ""cusi""<sint32>=<NULL>
    ""desc""<blob>=<NULL>
    ""gena""<blob>=<NULL>
    ""icmt""<blob>=<NULL>
    ""invi""<sint32>=<NULL>
    ""mdat""<timedate>=0x32303230303230393137343234395A00  ""20200209174249Z\000""
    ""nega""<sint32>=<NULL>
    ""prot""<blob>=<NULL>
    ""scrp""<sint32>=<NULL>
    ""svce""<blob>=""SFCCAccountManager""
    ""type""<uint32>=<NULL>

password: ""foo""";
            
            string username;
            var secret = SecretsConfigurationProvider.ParseSecurityOutput(output);
            
            Assert.Equal("foo", secret.Password);
            Assert.Equal("test@example.com", secret.Username);
        }
    }
}
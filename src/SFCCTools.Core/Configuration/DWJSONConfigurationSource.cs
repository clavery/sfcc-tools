using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace SFCCTools.Core.Configuration
{
    public class DWJSONConfigurationSource : JsonConfigurationSource
    {
        public DWJSONConfigurationSource(string path) : base()
        {
            this.Optional = true;
            this.Path = path;
            this.ReloadOnChange = false;
            this.FileProvider = null;
            this.ResolveFileProvider();
        }

        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new DWJSONConfigurationProvider(this);
        }
    }

    public class DWJSONConfigurationProvider : JsonConfigurationProvider
    {
        public DWJSONConfigurationProvider(JsonConfigurationSource source) : base(source) {}

        public override void Load()
        {
            base.Load();
            // normalize different key values
            if (this.Data.ContainsKey("hostname"))
            {
                this.Data.Add("Server", this.Data["hostname"]);
            }
            if (this.Data.ContainsKey("code-version"))
            {
                this.Data.Add("CodeVersion", this.Data["code-version"]);
            }
        }
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace SFCCTools.OCAPI.SharedTypes
{
    [JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(SnakeCaseNamingStrategy))]
    public enum StatusCode
    {
        Ok,
        Error
    }

    public class Status
    {
        [JsonProperty("status")]
        public StatusCode StatusCode;
        public string Code;
        public string Message;
    }
}
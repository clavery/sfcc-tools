using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SFCCTools.OCAPI.DataAPI.Types
{
    public class ListToCsvConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<string>);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(string.Join(",", (List<string>)value));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return new List<string>(((string)reader.Value).Split(','));
        }
    }
    
    public class OrganizationPreferences
    {
        // Naming of these custom properties due to compatibility with earlier (python) tools
        [JsonProperty("c_dwreMigrateCurrentVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string MigrateCurrentVersion;

        [JsonProperty("c_dwreMigrateToolVersion", NullValueHandling = NullValueHandling.Ignore)]
        public string MigrateToolVersion;

        [JsonConverter(typeof(ListToCsvConverter))]
        [JsonProperty("c_dwreMigrateVersionPath", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> MigrateVersionPath;
        
        [JsonConverter(typeof(ListToCsvConverter))]
        [JsonProperty("c_dwreMigrateHotfixes", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> MigrateHotfixes;
        
        [JsonExtensionData] public Dictionary<string, object> Custom { get; set; }
    }
}
using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SFCCTools.Core.Converters
{
    public class IPAddressConverter : JsonConverter<IPAddress>
    {
        public override IPAddress Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (IPAddress.TryParse(reader.GetString(), out var addr))
            {
                return addr;
            }
            else
            {
                return IPAddress.Any;
            }
        }

        public override void Write(
            Utf8JsonWriter writer,
            IPAddress value,
            JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
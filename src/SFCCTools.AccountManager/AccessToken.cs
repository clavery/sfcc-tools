using System;
using System.Text.Json.Serialization;

namespace SFCCTools.AccountManager
{
    public class AccessToken
    {
        public readonly string TokenType = "Bearer";

        public string ClientId { get; set; }
        
        public DateTime Expiration { get; set; }

        private int _expires;
        [JsonPropertyName("expires_in")]
        public int Expires
        {
            get => _expires;
            set
            {
                _expires = value;
                Expiration = DateTime.Now.AddSeconds(value);
            }
        }

        [JsonPropertyName("access_token")]
        public string Token { get; set; }

        public bool IsExpired()
        {
            return DateTime.Now.CompareTo(Expiration) >= 0;
        }
    }
}
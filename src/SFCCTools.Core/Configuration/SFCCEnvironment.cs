namespace SFCCTools.Core.Configuration
{
    public class SFCCEnvironmentException : System.Exception
    {
        public SFCCEnvironmentException(string message) : base(message) { }
    }

    public class SFCCEnvironment
    {
        public SFCCEnvironment()
        {
            Verify = true;
        }

        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
        public string CodeVersion { get; set; }
        // Verify SSL connections
        public bool Verify { get; set; }
        
        public string SiteID { get; set; }

        // checks for a valid environment (i.e. enough info to attempt log on to an instance)
        // does not actually check if the values are valid
        public bool IsValidEnvironment(bool throwOnInvalid = false)
        {
            bool valid = !string.IsNullOrEmpty(Server);

            if ((string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)) &&
                (string.IsNullOrEmpty(ClientID) || string.IsNullOrEmpty(ClientSecret)))
            {
                valid = false;
            }

            if (!valid && throwOnInvalid)
            {
                throw new SFCCEnvironmentException("A valid SFCC environment could not be derived; Ensure you've provided valid credentials in a configuration source.");
            }
            return valid;
        }

        public bool HasClientCredentials(bool throwOnInvalid = false)
        {
           var valid = !string.IsNullOrEmpty(ClientSecret) && !string.IsNullOrEmpty(ClientID);
           if (!valid && throwOnInvalid)
           {
                throw new SFCCEnvironmentException("A valid Client ID and secret are required");
           }

           return valid;
        }
    }

}
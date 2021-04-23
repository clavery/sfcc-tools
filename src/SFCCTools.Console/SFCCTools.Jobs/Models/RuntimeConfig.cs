using System;

namespace SFCCTools.Jobs
{
    /// <summary>
    /// Database stored runtime configuration (date and/or string values)
    /// </summary>
    public class RuntimeConfig
    {
        public string RuntimeConfigId { get; set; }
        public DateTime DateConfig { get; set; }
        public string StringConfig { get; set; }
    }
}
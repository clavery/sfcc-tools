using System;
using System.Collections.Generic;

namespace SFCCTools.OCAPI.DataAPI.Types
{
    public class CodeVersion
    {
        public string Id { get; set; }
        public bool Active { get; set; }
        public List<string> Cartridges { get; set; }
        public string CompatibilityMode { get; set; }
        public DateTime? ActivationTime { get; set; }
        public DateTime? LastModificationTime { get; set; }
        public bool? Rollback { get; set; }
        public long? TotalSize { get; set; }
        public string WebDavUrl { get; set; }
    }
}
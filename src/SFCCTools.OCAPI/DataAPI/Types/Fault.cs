using System;
using System.Collections.Generic;

namespace SFCCTools.OCAPI.DataAPI.Types
{
    public class OCAPIError
    {
        public Fault Fault;
    }

    public class Fault : Exception
    {
        public Dictionary<string, object> Arguments;
        public new string Message;
        public string Type;
        public string DisplayMessagePattern;
    }
}
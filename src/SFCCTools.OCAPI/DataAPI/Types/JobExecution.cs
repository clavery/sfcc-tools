using System;
using SFCCTools.OCAPI.SharedTypes;

namespace SFCCTools.OCAPI.DataAPI.Types
{
    public class JobExecution
    {
        public string Id;
        public string JobId;
        public DateTime StartTime;
        public DateTime EndTime;
        public Status ExitStatus;
    }
}
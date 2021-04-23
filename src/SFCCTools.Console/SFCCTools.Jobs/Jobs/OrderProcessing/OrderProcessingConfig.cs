using System;

namespace SFCCTools.Jobs
{
    public class OrderProcessingConfig
    {
        public OrderProcessingConfig()
        {
            InitialReferenceDate = new DateTime(2019, 12, 15);
            MaximumTimeSpan = TimeSpan.FromHours(1);
        }
        
        // Used when the last date of job processing not known
        public DateTime InitialReferenceDate { get; set; }
        
        // Maximum timespan process at one time
        public TimeSpan MaximumTimeSpan { get; set; }
    }
}
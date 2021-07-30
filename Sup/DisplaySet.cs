using System.Collections.Generic;

namespace SubtitleParser.Sup
{
    public class DisplaySet
    {
        public PCSData PCSData { get; set; }
        public List<WDSData> WDSDatas { get; set; } = new List<WDSData>();
        public List<ODSData> ODSDatas { get; set; } = new List<ODSData>();
        public List<PDSData> PDSDatas { get; set; } = new List<PDSData>();
    }
}

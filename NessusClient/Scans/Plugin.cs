using System.Collections.Generic;

namespace NessusClient.Scans
{
    public class Plugin
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Synopsis { get; set; }
        public string Solution { get; set; }
        public string ExploitAvailable { get; set; }
        public IEnumerable<string> ExploitableWith { get; set; }
        public IEnumerable<string> Cves { get; set; }
        public IEnumerable<string> Bids { get; set; }
        public IEnumerable<string> Xrefs { get; set; }
    }
}
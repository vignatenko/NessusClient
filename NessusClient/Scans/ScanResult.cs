using System.Collections.Generic;

namespace NessusClient.Scans
{
    public class ScanResult
    {
        public string Name { get; set; }
        public IEnumerable<Host> Hosts { get; set; }                            

    }
}
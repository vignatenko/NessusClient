using System;
using System.Collections.Generic;

namespace NessusClient.Scans
{
    public class Host
    {

        public string HostName { get; set; }
        public DateTimeOffset StartScanTime { get; set; }
        public DateTimeOffset EndScanTime { get; set; }
        public string NetbiosName { get; set; }
        public string Dns { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public string Os { get; set; }

      

        public IEnumerable<Vulnerability> Vulnerabilities { get; set; }
    }
}
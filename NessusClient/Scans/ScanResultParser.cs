using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace NessusClient.Scans
{
    class ScanResultParser
    {
        public static ScanResult Parse(Stream stream)
        {
            var doc = XDocument.Load(stream);
            if (doc?.Root == null)
                throw new NessusException("Invalid report");

            var root = doc.Root;
            var report = root.Element("Report");
            if (report == null)
                return null;
            var reportName = GetString(report.Attribute("name"));
            var hosts = report.Elements("ReportHost").Select(ParseHost).ToList();
            return new ScanResult
            {
                Name = reportName,
                Hosts = hosts
            };
        }
       
        private static Host ParseHost(XElement hostElem)
        {
            var host = new Host { HostName = GetString(hostElem.Attribute("name")) };
            Func<string, DateTime> parseDate = s =>
            {
                DateTime res;
                //Fri Aug 24 04:58:13 2012
                //Thu Oct  4 06:47:32 2012
                if (!DateTime.TryParseExact(s, "ddd MMM d HH:mm:ss yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out res))
                {
                    DateTime.TryParse(s, out res);
                }
                return res;
            };
            foreach (var tag in hostElem.Descendants("tag"))
            {
                var name = GetString(tag.Attribute("name"));
                var val = GetString(tag);
                switch (name)
                {
                    case "operating-system":
                        host.Os = val.Replace("\n", " - ");
                        break;
                    case "netbios-name":
                        host.NetbiosName = val;
                        break;
                    case "host-fqdn":
                        host.Dns = val;
                        break;
                    case "host-ip":
                        host.IpAddress = val;
                        break;
                    case "mac-address":
                        host.MacAddress = val;
                        break;
                    case "HOST_END":
                        host.EndScanTime = parseDate(val);
                        break;
                    case "HOST_START":
                        host.StartScanTime = parseDate(val);
                        break;
                }
            }
            host.Vulnerabilities = hostElem.Elements("ReportItem").Select(ParseReportItem).Where(ri => ri.Plugin.Id != "0").ToList();

            return host;
        }
        private static Vulnerability ParseReportItem(XElement itemElem)
        {
            var exploitableWith = new List<string>();
            int port;
            var v = new Vulnerability
            {
                Port = int.TryParse(GetString(itemElem.Attribute("port")), out port) ? port : 0,
                ServiceName = GetString(itemElem.Attribute("svc_name")),
                Protocol = GetString(itemElem.Attribute("protocol")),
                Severity = GetString(itemElem.Element("risk_factor")),
                PluginOutput = GetString(itemElem.Element("plugin_output")),
                SeeAlso = string.Join(Environment.NewLine, itemElem.Elements("see_also").Select(GetString)),
                Plugin = new Plugin
                {
                    Id = GetString(itemElem.Attribute("pluginID")),
                    Name = GetString(itemElem.Attribute("pluginName")),
                    Description = GetString(itemElem.Element("description")),
                    Synopsis = GetString(itemElem.Element("synopsis")),
                    Solution = GetString(itemElem.Element("solution")),
                    ExploitAvailable = GetString(itemElem.Element("exploit_available")),
                    ExploitableWith = exploitableWith,
                    Cves = itemElem.Elements("cve").Select(GetString).ToList(),
                    Bids = itemElem.Elements("bid").Select(GetString).ToList(),
                    Xrefs = itemElem.Elements("xref").Select(GetString).ToList()
                }
            };            

            return v;
        }
        private static string GetString(XElement elem)
        {
            return elem?.Value;
        }
        private static string GetString(XAttribute elem)
        {
            return elem?.Value;
        }
    }
}

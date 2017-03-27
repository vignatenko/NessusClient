using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NessusClient.Scans
{
    public static class NessusConnectionExtentions
    {
        public static async Task<IEnumerable<Scan>> GetScansAsync(this INessusConnection c)
        {                        
            using (var response = await c.CreateRequest("scans").GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                return ReadScans(stream);
                
            }           
        }

        public static async Task<IEnumerable<ScanHistory>> GetAllScanHistoriesAsync(this INessusConnection c)
        {
            var scans = await c.GetScansAsync();
            var result = new List<ScanHistory>();
            foreach (var scan in scans)
            {
                result.AddRange(await c.GetScanHistoryAsync(scan.Id));
            }
            return result;
        }

        public static async Task<IEnumerable<ScanHistory>> GetScanHistoryAsync(this INessusConnection c, int scanId)
        {
            NessusScanDetails obj;
            using (var response = await c.CreateRequest($"scans/{scanId}").GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                var js = new DataContractJsonSerializer(typeof(NessusScanDetails));
                obj = (NessusScanDetails)js.ReadObject(stream);
            }
            var scanHistory = obj.History;
            return scanHistory.Select(
                historyItem =>
                    new ScanHistory(scanId, 
                        obj.Info.Name,
                        historyItem.LastModificationDate, historyItem.HistoryId));
        }
        public static async Task<int> BeginExportAsync(this INessusConnection c, int scanId, int historyId, ExportFormat exportFormat = ExportFormat.Nessus)
        {
            var req = c.CreateRequest($"scans/{scanId}/export", WebRequestMethods.Http.Post);
                        
            using (var stream = await req.GetRequestStreamAsync())
            {
                var exportRequest = new NessusExportFileRequest
                {
                    HistoryId = historyId,
                    Format = Enum.GetName(typeof(ExportFormat), exportFormat).ToLowerInvariant()
                };

                if (exportFormat == ExportFormat.Html || exportFormat == ExportFormat.Pdf)
                {
                    exportRequest.Chapters = "vuln_hosts_summary";
                }
                var js = new DataContractJsonSerializer(typeof(NessusExportFileRequest));
                js.WriteObject(stream, exportRequest);
                
            }

            var res = (HttpWebResponse)await req.GetResponseAsync();

            switch (res.StatusCode)
            {
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.NotFound:
                    throw new NessusException($"Scan with id = {scanId} is not found");
                default:
                    throw new NessusException($"HTTP {res.StatusCode}. {res.StatusDescription ?? string.Empty}.");
            }

            NessusExportFile exportFile;
            using (var stream = res.GetResponseStream())
            {
                var js = new DataContractJsonSerializer(typeof(NessusExportFile));
                
                exportFile = (NessusExportFile)js.ReadObject(stream);
            }
            return exportFile.File;
        }

        public static async Task<bool> IsExportCompletedAsync(this INessusConnection c, int scanId, int fileId)
        {
            var statReq = c.CreateRequest($"scans/{scanId}/export/{fileId}/status");

            using (var statRes = (HttpWebResponse) await statReq.GetResponseAsync())
            {
                switch (statRes.StatusCode)
                {
                    case HttpStatusCode.OK:
                        break;
                    case HttpStatusCode.NotFound:
                        throw new NessusException($"File with id = {fileId} does not exist");

                    default:
                        throw new NessusException(
                            $"HTTP {statRes.StatusCode}. {statRes.StatusDescription ?? string.Empty}.");
                }

                using (var stream = statRes.GetResponseStream())
                {
                    var js = new DataContractJsonSerializer(typeof(NessusExportFileStatus));
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var obj = (NessusExportFileStatus)js.ReadObject(stream);
                    return obj.Status == "ready";
                }
            }
        }
        public static async Task DownloadAsync(this INessusConnection c, int scanId, int fileId, Stream targetStream)
        {            
            using (var downloadStream = await c.DownloadAsync(scanId, fileId))
            {
                await downloadStream.CopyToAsync(targetStream);
            }
        }
        

        public static async Task ExportAsync(this INessusConnection c,
            int scanId,
            int historyId,
            CancellationToken  cancellationToken,
            ExportFormat exportFormat,
            Stream targetStream)
        {
            var fileId = await c.BeginExportAsync(scanId, historyId, exportFormat);

            await c.WaitForExportCompletion(scanId, fileId, cancellationToken);

            await c.DownloadAsync(scanId, fileId, targetStream);
        }

        public static async Task<ScanResult> GetScanResultAsync(this INessusConnection c,
           int scanId,
           int historyId,
           CancellationToken cancellationToken)
        {
            var fileId = await c.BeginExportAsync(scanId, historyId);

            await c.WaitForExportCompletion(scanId, fileId, cancellationToken);
            
            using (var stream = await c.DownloadAsync(scanId, fileId))
            {
                return ScanResultParser.Parse(stream);
            }
            
        }
       
        private static async Task WaitForExportCompletion(this INessusConnection c,
            int scanId,
            int fileId,
            CancellationToken cancellationToken)
        {
            const int timeoutBetweenAttempts = 2000;

            for (;;)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await c.IsExportCompletedAsync(scanId, fileId))
                    break;
                await Task.Delay(timeoutBetweenAttempts, cancellationToken);
            }
        }
        private static async Task<Stream> DownloadAsync(this INessusConnection c, int scanId, int fileId)
        {
            var downloadReq = c.CreateRequest($"scans/{scanId}/export/{fileId}/download");

            var downloadRes = (HttpWebResponse)await downloadReq.GetResponseAsync();

            switch (downloadRes.StatusCode)
            {
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.NotFound:
                    throw new NessusException($"File with id = {fileId} does not exist");

                default:
                    throw new NessusException(
                        $"HTTP {downloadRes.StatusCode}. {downloadRes.StatusDescription ?? string.Empty}.");
            }

            return downloadRes.GetResponseStream();

        }       
        
        private static IEnumerable<Scan> ReadScans(Stream reader)
        {


            var js = new DataContractJsonSerializer(typeof(NessusScanList));
            var obj = (NessusScanList)js.ReadObject(reader);
            return
                obj.Scans.Where(x => x.Status == "completed" || x.Status == "imported")
                    .Select(x => new Scan(x.Id, x.Name, x.LastModificationDate))
                    .ToList();

        }
    }
}

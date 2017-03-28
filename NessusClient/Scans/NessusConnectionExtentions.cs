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
        
        public static async Task<IEnumerable<Scan>> GetScansAsync(this INessusConnection conn, CancellationToken cancellationToken)
        {                        
            using (var response = await conn.CreateRequest("scans", WebRequestMethods.Http.Get, cancellationToken).GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                return ReadScans(stream);               
            }           
        }

        public static async Task<IEnumerable<ScanHistory>> GetAllScanHistoriesAsync(this INessusConnection conn, CancellationToken cancellationToken)
        {
            var scans = await conn.GetScansAsync(cancellationToken);
            var result = new List<ScanHistory>();            
            foreach (var scan in scans)
            {
                result.AddRange(await conn.GetScanHistoryAsync(scan.Id, cancellationToken));
            }
            return result;
        }

        public static async Task<IEnumerable<ScanHistory>> GetScanHistoryAsync(this INessusConnection conn, int scanId, CancellationToken cancellationToken)
        {
            NessusScanDetails obj;
            using (var response = await conn.CreateRequest($"scans/{scanId}", WebRequestMethods.Http.Get, cancellationToken).GetResponseAsync())
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
        public static async Task<int> BeginExportAsync(this INessusConnection conn, int scanId, int historyId, ExportFormat exportFormat, CancellationToken cancellationToken)
        {
            var req = conn.CreateRequest($"scans/{scanId}/export", WebRequestMethods.Http.Post, cancellationToken);
                        
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

        public static async Task<bool> IsExportCompletedAsync(this INessusConnection conn, int scanId, int fileId, CancellationToken cancellationToken)
        {
            var statReq = conn.CreateRequest($"scans/{scanId}/export/{fileId}/status", WebRequestMethods.Http.Get, cancellationToken);

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
        public static async Task DownloadAsync(this INessusConnection conn, int scanId, int fileId, Stream targetStream, CancellationToken cancellationToken)
        {            
            using (var downloadStream = await conn.DownloadAsync(scanId, fileId, cancellationToken))
            {
                await downloadStream.CopyToAsync(targetStream);
            }
        }


        public static async Task ExportAsync(this INessusConnection conn,
            int scanId,
            int historyId,
            ExportFormat exportFormat,
            Stream targetStream,
            CancellationToken cancellationToken)
        {
            var fileId = await conn.BeginExportAsync(scanId, historyId, exportFormat, cancellationToken);

            await conn.WaitForExportCompletion(scanId, fileId, cancellationToken);

            await conn.DownloadAsync(scanId, fileId, targetStream, cancellationToken);
        }

        public static async Task<ScanResult> GetScanResultAsync(this INessusConnection conn,
           int scanId,
           int historyId,
           CancellationToken cancellationToken)
        {
            var fileId = await conn.BeginExportAsync(scanId, historyId, ExportFormat.Nessus, cancellationToken);

            await conn.WaitForExportCompletion(scanId, fileId, cancellationToken);
            
            using (var stream = await conn.DownloadAsync(scanId, fileId, cancellationToken))
            {
                return ScanResultParser.Parse(stream);
            }
            
        }
       
        private static async Task WaitForExportCompletion(this INessusConnection conn,
            int scanId,
            int fileId,
            CancellationToken cancellationToken)
        {
            const int timeoutBetweenAttempts = 2000;

            for (;;)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await conn.IsExportCompletedAsync(scanId, fileId, cancellationToken))
                    break;
                await Task.Delay(timeoutBetweenAttempts, cancellationToken);
            }
        }
        private static async Task<Stream> DownloadAsync(this INessusConnection conn, int scanId, int fileId, CancellationToken cancellationToken)
        {
            var downloadReq = conn.CreateRequest($"scans/{scanId}/export/{fileId}/download", WebRequestMethods.Http.Get, cancellationToken);

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
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
                // ReSharper disable once AssignNullToNotNullAttribute
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
        public static async Task<Scan> ImportAsync(this INessusConnection c, string filePath)
        {
            var serverFileName = await c.UploadFileAsync( filePath);
            var request = c.CreateRequest("scans/import", WebRequestMethods.Http.Post);
            using (var rs = request.GetRequestStream())
            {
                var bytes = Encoding.UTF8.GetBytes($"{{\"file\": \"{serverFileName}\"}}");
                await rs.WriteAsync(bytes, 0, bytes.Length);
            }
            NessusImportResult importResult;
            using (var responseStream = (await request.GetResponseAsync()).GetResponseStream())
            {
                var js = new DataContractJsonSerializer(typeof(NessusImportResult));
                importResult = (NessusImportResult) js.ReadObject(responseStream);
            }
            return new Scan(importResult.Scan.Id, importResult.Scan.Name, importResult.Scan.LastModificationDate );
            
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
        private static async Task<string> UploadFileAsync(this INessusConnection c,  string filePath)
        {
            string serverFileName;
            var request = c.CreateRequest("file/upload", WebRequestMethods.Http.Post);

            var encoding = Encoding.UTF8;
            var boundary = "------------------------" + DateTime.Now.Ticks;


            var fileHeaderFormat =
                $"--{boundary}\r\nContent-Disposition: form-data; name=\"Filedata\"; filename=\"{Path.GetFileName(filePath)}\";\r\nContent-Type: application/octet-stream\r\n\r\n";

            request.ContentType = "multipart/form-data; boundary=" + boundary;


            using (var stream = await request.GetRequestStreamAsync())
            {
                var data = encoding.GetBytes(fileHeaderFormat);
                await stream.WriteAsync(data, 0, data.Length);

                using (var sourceStream = File.OpenRead(filePath))
                {
                    await sourceStream.CopyToAsync(stream);
                }
                await stream.FlushAsync();
            }
            using (var response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new NessusException($"Unable to upload file {filePath}. Error: {response.StatusCode}");
                }

                using (var stream = response.GetResponseStream())
                {
                    var js = new DataContractJsonSerializer(typeof(NessusReportUploadStatus));
                    
                    var obj = (NessusReportUploadStatus)js.ReadObject(stream);
                    serverFileName = obj.FileUploaded;
                }
            }

            return serverFileName;
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

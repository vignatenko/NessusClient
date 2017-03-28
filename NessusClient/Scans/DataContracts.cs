using System.Runtime.Serialization;

namespace NessusClient.Scans
{
    [DataContract]
    internal class NessusSessionToken
    {
        [DataMember(Name = "token")]
        public string Token { get; set; }
    }
    [DataContract]
    internal class NessusScanList
    {
        [DataMember(Name = "scans")]
        public NessusScanListItem[] Scans { get; set; }
    }
    [DataContract]
    internal class NessusImportResult
    {
        [DataMember(Name = "scan")]
        public NessusScanListItem Scan { get; set; }
    }

    [DataContract]
    internal class NessusScanListItem
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "uuid")]
        public string Uuid { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "last_modification_date")]
        public long LastModificationDate { get; set; }

    }
    [DataContract]
    internal class NessusScanDetails
    {
        [DataContract]
        public class ScanInfo
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }
        }
        [DataMember(Name = "info")]
        public ScanInfo Info { get; set; }

        [DataMember(Name = "history")]
        public NessusScanHistoryItem[] History { get; set; }

    }

    [DataContract]
    internal class NessusScanHistoryItem
    {
        [DataMember(Name = "history_id")]
        public int HistoryId { get; set; }

        [DataMember(Name = "uuid")]
        public string Uuid { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "last_modification_date")]
        public long LastModificationDate { get; set; }
    }

    [DataContract]
    internal class NessusExportFileRequest
    {
        [DataMember(Name = "format")]
        public string Format { get; set; }

        [DataMember(Name = "history_id")]
        public int HistoryId { get; set; }

        [DataMember(Name = "chapters")]
        public string Chapters { get; set; }

    }
    [DataContract]
    internal class NessusExportFile
    {
        [DataMember(Name = "file")]
        public int File { get; set; }

    }
    [DataContract]
    internal class NessusExportFileStatus
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }

    }

    [DataContract]
    internal class NessusReportUploadStatus
    {
        [DataMember(Name = "fileuploaded")]
        public string FileUploaded { get; set; }

    }
    [DataContract]
    internal class NessusScanCreationResponse
    {
        [DataContract]
        public class ScanData
        {
            [DataMember(Name = "id")]
            public int Id { get; set; }
        }

        [DataMember(Name = "scan")]
        public ScanData Scan { get; set; }

    }
}

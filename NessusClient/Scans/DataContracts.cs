using System.Runtime.Serialization;

namespace NessusClient.Scans
{
    [DataContract]
    class Nessus6SessionToken
    {
        [DataMember(Name = "token")]
        public string Token { get; set; }
    }
    [DataContract]
    class NessusScanList
    {
        [DataMember(Name = "scans")]
        public NessusScanListItem[] Scans { get; set; }
    }
    [DataContract]
    class NessusImportResult
    {
        [DataMember(Name = "scan")]
        public NessusScanListItem Scan { get; set; }
    }

    [DataContract]
    class NessusScanListItem
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
    class NessusScanDetails
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
    class NessusScanHistoryItem
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
    class NessusExportFileRequest
    {
        [DataMember(Name = "format")]
        public string Format { get; set; }

        [DataMember(Name = "history_id")]
        public int HistoryId { get; set; }

        [DataMember(Name = "chapters")]
        public string Chapters { get; set; }

    }
    [DataContract]
    class NessusExportFile
    {
        [DataMember(Name = "file")]
        public int File { get; set; }

    }
    [DataContract]
    class NessusExportFileStatus
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }

    }

    [DataContract]
    class NessusReportUploadStatus
    {
        [DataMember(Name = "fileuploaded")]
        public string FileUploaded { get; set; }

    }
    [DataContract]
    class NessusScanCreationResponse
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

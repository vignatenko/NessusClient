namespace NessusClient.Scans
{
    public class ScanHistory : Scan
    {
        public int HistoryId { get; set; }

        public ScanHistory(int id, string name, long timestamp, int historyId) : base(id, name, timestamp)
        {
            HistoryId = historyId;
        }
    }
}
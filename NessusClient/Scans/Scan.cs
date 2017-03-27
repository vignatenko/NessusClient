using System;

namespace NessusClient.Scans
{
    public class Scan
    {

        public int Id { get; }        
        public string Name { get; }
        public DateTimeOffset LastUpdateDate { get; }

        /// <summary>Constructor</summary>
        /// <param name="id">report identifier</param>
        /// <param name="name">report name</param>
        /// <param name="timestamp">timestamp in UNIX time</param>        
        public Scan(int id, string name, long timestamp)
        {
            // common static formatter
            Id = id;
            Name = name;
            LastUpdateDate  = timestamp == 0 ? DateTimeOffset.MinValue : new DateTimeOffset(1970, 1, 1, 0, 0, 0, 0, TimeSpan.Zero).AddSeconds(timestamp);            

        }             
    }
}
using System;
using System.Collections.Generic;

namespace AppCleanerPro.Models
{
    public class ProcessItem
    {
        public int ProcessId { get; set; }
        public string Name { get; set; } = string.Empty;
        public long MemoryBytes { get; set; }
        public string MemoryDisplay { get; set; } = string.Empty;
        public string MainWindowTitle { get; set; } = string.Empty;
    }

    public class NetworkConnectionItem
    {
        public string Protocol { get; set; } = string.Empty;
        public string LocalAddress { get; set; } = string.Empty;
        public string RemoteAddress { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public class PrivacyMatch
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int MatchCount { get; set; }
        public long SizeBytes { get; set; }
        public string SizeDisplay { get; set; } = string.Empty;
    }
}

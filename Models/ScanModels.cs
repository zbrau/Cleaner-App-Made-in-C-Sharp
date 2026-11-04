using System;
using System.Collections.Generic;

namespace AppCleanerPro.Models
{
    public class CategoryResult
    {
        public long Size { get; set; }
        public int Count { get; set; }
        public List<string> Files { get; set; } = new();
        public bool IsRecycleBin { get; set; }
        public bool IsPrivacy { get; set; }
    }

    public class ScanResult
    {
        public Dictionary<string, CategoryResult> Categories { get; set; } = new();
        public DateTime ScannedAt { get; set; } = DateTime.Now;
        public long TotalSize => Categories.Values.Sum(c => c.Size);
    }

    public class CleanCategory
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsSelected { get; set; } = true;
        public string SizeDisplay { get; set; } = "—";
        public long SizeBytes { get; set; }
        public string AccentColor { get; set; } = "#7B9FFF";
    }

    public class CleanHistoryEntry
    {
        public string Date { get; set; } = "";
        public long Timestamp { get; set; }
        public long Bytes { get; set; }
    }
}

using System.Collections.Generic;

namespace AppCleanerPro.Models
{
    public class DiskItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public double PercentOfParent { get; set; }
        
        // Display properties for UI binding
        public string DisplaySize => Core.CleanerEngine.FormatSize(SizeBytes);
    }

    public class DuplicateGroup
    {
        public string Hash { get; set; } = "";
        public long SizeBytes { get; set; }
        public List<DuplicateFileItem> Files { get; set; } = new();

        public string GroupTitle => $"Archivos idénticos — {Core.CleanerEngine.FormatSize(SizeBytes)} c/u";
    }

    public class DuplicateFileItem
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string Directory => System.IO.Path.GetDirectoryName(Path) ?? "";
        public bool IsSelected { get; set; } = true; // Auto-select for deletion except one
    }
}

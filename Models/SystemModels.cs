using System.Collections.Generic;

namespace AppCleanerPro.Models
{
    public class SystemHardwareInfo
    {
        public string OsName { get; set; } = "";
        public string OsArchitecture { get; set; } = "";
        public string CpuName { get; set; } = "";
        public int CpuCores { get; set; }
        public string GpuName { get; set; } = "";
        public string RamTotal { get; set; } = "";
        public string MoboModel { get; set; } = "";
    }

    public class AppSettings
    {
        public bool AutoScanOnStartup { get; set; } = false;
        public bool CloseAfterClean { get; set; } = false;
        public bool CheckForUpdates { get; set; } = true;
        public bool ConfirmDeletions { get; set; } = true;
        public string Theme { get; set; } = "Dark";
        public bool SoundEffects { get; set; } = true;
        public string AccentColor { get; set; } = "ElectricBlue";
    }
}

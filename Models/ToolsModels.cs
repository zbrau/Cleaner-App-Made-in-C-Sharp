using System;

namespace AppCleanerPro.Models
{
    public class AppItem
    {
        public string Name { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string SizeDisplay { get; set; } = string.Empty;
        public string UninstallString { get; set; } = string.Empty;
        public string RegistryKeyPath { get; set; } = string.Empty;
    }

    public class RegistryIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RootKey { get; set; } = string.Empty;
        public string SubKey { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public class FirewallRuleItem
    {
        public string Name { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public string Protocol { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public string ActionColor => Action == "Allow" ? "#10b981" : "#ef4444";
        public string DirectionColor => Direction == "Inbound" ? "#3b82f6" : "#8b5cf6";
    }

    public class StartupItem
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty;
    }
}

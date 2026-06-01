using Licenta.Models.Security;
using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalCoaches { get; set; }
        public int TotalStaff { get; set; }

        public double RamUsageMb { get; set; } 
        public double RamTotalMb { get; set; } 
        public int RamPercentage => (int)((RamUsageMb / RamTotalMb) * 100);

        public double StorageUsageMb { get; set; } 
        public double StorageLimitMb { get; set; } 
        public int StoragePercentage => (int)((StorageUsageMb / StorageLimitMb) * 100);

        public int ActiveSessions { get; set; } 
        public string LastActivityTime { get; set; }

        public int CpuUsagePercent { get; set; } 

        public List<AuditLog> RecentLogs { get; set; }
    }
}
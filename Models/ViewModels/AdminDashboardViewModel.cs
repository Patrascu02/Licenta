using Licenta.Models.Security;
using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        // Contoare Generale
        public int TotalUsers { get; set; }
        public int TotalPlayers { get; set; }
        public int TotalCoaches { get; set; }
        public int TotalStaff { get; set; }

        // Monitorizare Sistem (Metrici Reale)
        public double RamUsageMb { get; set; } // RAM folosit de app
        public double RamTotalMb { get; set; } // RAM alocat (limită fictivă sau reală)
        public int RamPercentage => (int)((RamUsageMb / RamTotalMb) * 100);

        public double StorageUsageMb { get; set; } // Mărimea folderului wwwroot
        public double StorageLimitMb { get; set; } // Limită hosting (ex: 500MB)
        public int StoragePercentage => (int)((StorageUsageMb / StorageLimitMb) * 100);

        public int ActiveSessions { get; set; } // Useri activi în ultimele 30 min
        public string LastActivityTime { get; set; }

        public int CpuUsagePercent { get; set; } // Simulat sau calculat

        // Lista Log-uri
        public List<AuditLog> RecentLogs { get; set; }
    }
}
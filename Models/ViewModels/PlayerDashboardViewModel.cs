using Licenta.Models.Contracts;
using Licenta.Models.Core;
using Licenta.Models.Medical;
using Licenta.Models.Roles;
using Licenta.Models.Sports;
using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class PlayerDashboardViewModel
    {
        public Staff StaffInfo { get; set; }
        public Player PlayerInfo { get; set; }
        public Contract ActiveContract { get; set; }

        
        public double AvgPoints { get; set; }
        public double AvgRebounds { get; set; }
        public double AvgAssists { get; set; }
        public int GamesPlayed { get; set; }

       
        public List<PlayerGameStats> RecentPerformances { get; set; }
        public List<Game> UpcomingGames { get; set; }
        public List<Injury> ActiveInjuries { get; set; }

        public Licenta.Models.Calendar.Event NextEvent { get; set; }
    }
}
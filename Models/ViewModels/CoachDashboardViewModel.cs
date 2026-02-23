using Licenta.Models.Core;
using Licenta.Models.Roles;
using Licenta.Models.Sports;
using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class CoachDashboardViewModel
    {
        public Staff StaffInfo { get; set; }
        public Coach CoachInfo { get; set; }
        public List<Game> UpcomingGames { get; set; }
        public List<Game> RecentGames { get; set; }
        public int TotalPlayers { get; set; }
        public int InjuredPlayersCount { get; set; }
        public Licenta.Models.Calendar.Event NextEvent { get; set; }
    }
}
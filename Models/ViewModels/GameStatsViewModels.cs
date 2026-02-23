using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class EditGameStatsViewModel
    {
        public int GameId { get; set; }
        public string GameInfo { get; set; }

        public int HomeScore { get; set; }
        public int AwayScore { get; set; }

        public List<PlayerStatItemViewModel> PlayerStats { get; set; } = new List<PlayerStatItemViewModel>();
    }

    public class PlayerStatItemViewModel
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Position { get; set; }
        public int? JerseyNumber { get; set; }

        public int PlayerGameStatsId { get; set; } 
        public double Points { get; set; }
        public double Rebounds { get; set; }
        public double Assists { get; set; }
        public double Steals { get; set; }
        public double Blocks { get; set; }
        public double MinutesPlayed { get; set; }
    }
}
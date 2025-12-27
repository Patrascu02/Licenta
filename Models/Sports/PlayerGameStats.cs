using Licenta.Models.Roles;
using Licenta.Models.Sports;

namespace Licenta.Models.Sports
{
    public class PlayerGameStats
    {
        public int PlayerGameStatsId { get; set; }
        public int GameId { get; set; }
        public int PlayerId { get; set; }
        public int Points { get; set; }
        public int Assists { get; set; }
        public int Rebounds { get; set; }
        public int MinutesPlayed { get; set; }
        public float Efficiency { get; set; }

        public Game Game { get; set; }
        public Player Player { get; set; }
    }
}

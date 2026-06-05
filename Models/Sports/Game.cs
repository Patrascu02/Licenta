namespace Licenta.Models.Sports
{
    public class Game
    {
        public int GameId { get; set; }
        public int SeasonId { get; set; }
        public DateTime GameDate { get; set; }
        public string? Location { get; set; }

        public string? OpponentName { get; set; }
        public bool IsHomeGame { get; set; } 
        public int ClubScore { get; set; }
        public int OpponentScore { get; set; }

        public Season? Season { get; set; }
        public ICollection<PlayerGameStats>? PlayerStats { get; set; }
    }
}
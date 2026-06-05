using System;
using System.Collections.Generic;

namespace Licenta.Models.Sports
{
    public class Game
    {
        public int GameId { get; set; }
        public int SeasonId { get; set; }
        public DateTime GameDate { get; set; }
        public string? Location { get; set; }
        public string? GameType { get; set; }

        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }

        public int HomeScore { get; set; }
        public int AwayScore { get; set; }

        public Season? Season { get; set; }
        public Team? HomeTeam { get; set; }
        public Team? AwayTeam { get; set; }

        public ICollection<PlayerGameStats>? PlayerStats { get; set; }
    }
}
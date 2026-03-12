using Licenta.Models.Roles;
using Licenta.Models.Sports;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class PlayerGameStats
{
    public int PlayerGameStatsId { get; set; }
    public int PlayerId { get; set; }

    [ValidateNever]
    public Player Player { get; set; }

    public int? GameId { get; set; }
    public Game? Game { get; set; }

    public int Month { get; set; } 
    public int Year { get; set; }
    public bool IsScoutingReport { get; set; } 

    
    public double Points { get; set; }
    public double Rebounds { get; set; }
    public double Assists { get; set; }
    public double Blocks { get; set; }
    public double Steals { get; set; }
    public double MinutesPlayed { get; set; }
}
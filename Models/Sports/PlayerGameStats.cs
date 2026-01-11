using Licenta.Models.Roles;
using Licenta.Models.Sports;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class PlayerGameStats
{
    public int PlayerGameStatsId { get; set; }
    public int PlayerId { get; set; }

    [ValidateNever]//valideaza id ul dar nu te uita la tot obiectul player
    public Player Player { get; set; }

    // Facem GameId nullable pentru că un raport de scouting lunar 
    // reprezintă media mai multor meciuri, nu a unuia singur
    public int? GameId { get; set; }
    public Game? Game { get; set; }

    // ADAUGĂ ACESTE CÂMPURI PENTRU SCOUTING LUNAR:
    public int Month { get; set; } // 1-12
    public int Year { get; set; }
    public bool IsScoutingReport { get; set; } // Diferențiem un meci real de o notă de scouting

    
    public double Points { get; set; }
    public double Rebounds { get; set; }
    public double Assists { get; set; }
    public double Blocks { get; set; }
    public double Steals { get; set; }
    public double MinutesPlayed { get; set; }
}
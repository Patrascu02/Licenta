using Licenta.Models.Roles;

namespace Licenta.Models.Sports
{
    public class Team
    {
        public int TeamId { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? Category { get; set; }

        public int MaxAge { get; set; }

        public decimal BudgetLimit { get; set; } = 1000000;

        public ICollection<PlayerTeamHistory>? PlayerHistories { get; set; }
    }
}

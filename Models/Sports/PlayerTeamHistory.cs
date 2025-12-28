using Licenta.Models.Roles;

namespace Licenta.Models.Sports
{
    public class PlayerTeamHistory
    {
        public int PlayerTeamHistoryId { get; set; }
        public int PlayerId { get; set; }
        public int TeamId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public Player? Player { get; set; }
        public Team? Team { get; set; }
    }
}

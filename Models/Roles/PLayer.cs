using Licenta.Models.Core;
using Licenta.Models.Sports;
using Licenta.Models.Medical;
using Licenta.Models.Contracts;

namespace Licenta.Models.Roles
{
    public class Player
    {
        public int PlayerId { get; set; }
        public int StaffId { get; set; }
        public string? Position { get; set; }
        public int JerseyNumber { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }

        public Staff? Staff { get; set; }
        public ICollection<PlayerTeamHistory>? TeamHistories { get; set; }
        public ICollection<PlayerGameStats>? GameStats { get; set; }
        public ICollection<Contract>? Contracts { get; set; }
        public ICollection<Injury>? Injuries { get; set; }
    }
}
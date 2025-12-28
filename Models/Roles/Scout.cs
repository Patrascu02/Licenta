using Licenta.Models.Core;
using Licenta.Models.Scouting;

namespace Licenta.Models.Roles
{
    public class Scout
    {
        public int ScoutId { get; set; }
        public int StaffId { get; set; }
        public string? Region { get; set; }

        public Staff? Staff { get; set; }
        public ICollection<ScoutPlayer>? ScoutPlayers { get; set; }
    }
}

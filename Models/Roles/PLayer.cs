using Licenta.Models.Contracts;
using Licenta.Models.Core;
using Licenta.Models.Medical;
using Licenta.Models.Sports;
using System.ComponentModel.DataAnnotations.Schema;

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

        public int? CurrentTeamId { get; set; }

        public Staff? Staff { get; set; }


        [ForeignKey("CurrentTeamId")]
        public Team CurrentTeam { get; set; }
        public ICollection<PlayerTeamHistory>? TeamHistories { get; set; }
        public ICollection<PlayerGameStats>? GameStats { get; set; }
        public ICollection<Injury>? Injuries { get; set; }

        public DateTime? LastMedicalClearance { get; set; }
    }
}
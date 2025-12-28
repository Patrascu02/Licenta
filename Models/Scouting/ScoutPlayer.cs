using Licenta.Models.Roles;

namespace Licenta.Models.Scouting
{
    public class ScoutPlayer
    {
        public int ScoutPlayerId { get; set; }
        public int ScoutId { get; set; }
        public string? PlayerName { get; set; }
        public string? Observations { get; set; } // "arunca prost", "slab psihic" etc.

        public Scout? Scout { get; set; }
    }
}

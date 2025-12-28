using Licenta.Models.Core;

namespace Licenta.Models.Roles
{
    public class Coach
    {
        public int CoachId { get; set; }
        public int StaffId { get; set; }
        public string? LicenseNumber { get; set; }

        public Staff? Staff { get; set; }
    }
}

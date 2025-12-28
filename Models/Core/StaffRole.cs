using Licenta.Models.Identity;
using Microsoft.AspNetCore.Identity;

namespace Licenta.Models.Core
{
    public class StaffRole
    {
        public int StaffRoleId { get; set; }
        public int StaffId { get; set; }
        public string RoleId { get; set; }

        public Staff Staff { get; set; }

        public IdentityRole Role { get; set; }
    }
}
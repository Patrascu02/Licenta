using Licenta.Models.Security;
using Licenta.Models.Identity;

namespace Licenta.Models.Security
{
    public class RolePermission
    {
        public int RolePermissionId { get; set; }
        public string RoleId { get; set; }
        public int PermissionId { get; set; }

        public AspNetRoles Role { get; set; }
        public Permission Permission { get; set; }
    }
}

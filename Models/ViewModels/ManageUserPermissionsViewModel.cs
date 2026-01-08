using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class ManageUserPermissionsViewModel
    {
        public int StaffId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<PermissionCheckbox> PermissionList { get; set; }
    }
}
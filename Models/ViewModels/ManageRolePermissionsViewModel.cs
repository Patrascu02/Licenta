namespace Licenta.Models.ViewModels
{
    public class ManageRolePermissionsViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public List<PermissionCheckbox> PermissionList { get; set; }
    }

    public class PermissionCheckbox
    {
        public int PermissionId { get; set; }
        public string Name { get; set; }       // Ex: "Contracts.Edit"
        public string Description { get; set; } // Ex: "Poate modifica contracte"
        public bool IsSelected { get; set; }

        public bool IsInherited { get; set; } // Vine automat din Rol (RolePermission)
    }
}
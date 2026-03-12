using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta.Models.Security
{
    public class UserPermission
    {
        [Key]
        public int UserPermissionId { get; set; }

        [Required]
        public string UserId { get; set; }

        public int PermissionId { get; set; }

        [ForeignKey("PermissionId")]
        public Permission Permission { get; set; }
    }
}
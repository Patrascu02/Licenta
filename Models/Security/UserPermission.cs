using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licenta.Models.Security
{
    public class UserPermission
    {
        [Key]
        public int UserPermissionId { get; set; }

        // Aici folosim UserId ca simplu string pentru a evita ciclul (Multiple Cascade Paths)
        [Required]
        public string UserId { get; set; }

        // Legătura cu permisiunea (Foreign Key)
        public int PermissionId { get; set; }

        [ForeignKey("PermissionId")]
        public Permission Permission { get; set; }
    }
}
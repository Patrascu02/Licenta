using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.ViewModels
{
    public class EditUserViewModel
    {
        public int StaffId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [DataType(DataType.Date)]
        public DateTime HireDate { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.ViewModels
{
    public class EditUserViewModel
    {
        public int StaffId { get; set; }
        public string RoleName { get; set; }

        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [Display(Name = "Nume")]
        public string LastName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data Nașterii")]
        public DateTime DateOfBirth { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data Angajării")]
        public DateTime HireDate { get; set; }

        [Display(Name = "Poziție")]
        public string? Position { get; set; }

        [Display(Name = "Număr Tricou")]
        public int? JerseyNumber { get; set; }

        [Display(Name = "Înălțime (cm)")]
        public int? Height { get; set; }

        [Display(Name = "Greutate (kg)")]
        public int? Weight { get; set; }

        [Display(Name = "Licență")]
        public string? LicenseNumber { get; set; }

        [Display(Name = "Specializare")]
        public string? Specialization { get; set; }

        [Display(Name = "Birou")]
        public string? Office { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.ViewModels
{
    public class EditUserViewModel
    {
        public int StaffId { get; set; }
        public string RoleName { get; set; } // Avem nevoie de asta pentru a ști ce câmpuri afișăm

        [Required(ErrorMessage = "Prenumele este obligatoriu")]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Numele este obligatoriu")]
        [Display(Name = "Nume")]
        public string LastName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; } // Read-only de obicei

        [DataType(DataType.Date)]
        [Display(Name = "Data Nașterii")]
        public DateTime DateOfBirth { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data Angajării")]
        public DateTime HireDate { get; set; }

        // --- Date Specifice Jucător ---
        [Display(Name = "Poziție")]
        public string? Position { get; set; }

        [Display(Name = "Număr Tricou")]
        public int? JerseyNumber { get; set; }

        [Display(Name = "Înălțime (cm)")]
        public int? Height { get; set; }

        // --- Date Specifice Antrenor ---
        [Display(Name = "Licență")]
        public string? LicenseNumber { get; set; }

        // --- Date Specifice Medic ---
        [Display(Name = "Specializare")]
        public string? Specialization { get; set; }
    }
}
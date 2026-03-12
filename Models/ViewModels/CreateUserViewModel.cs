using System;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Formatul email-ului este invalid.")]
        [Display(Name = "Email Oficial")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parola este obligatorie.")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$",
            ErrorMessage = "Parola trebuie să aibă minim 6 caractere, o cifră și o majusculă.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmarea parolei este obligatorie.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Parolele nu coincid.")]
        [Display(Name = "Confirmă Parola")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selectarea unui rol este obligatorie.")]
        public string Role { get; set; } = string.Empty;


        [Required(ErrorMessage = "Prenumele este obligatoriu.")]
        [Display(Name = "Prenume")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Numele este obligatoriu.")]
        [Display(Name = "Nume de Familie")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Data nașterii este obligatorie.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data Nașterii")]
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-20);

        [Required(ErrorMessage = "Data angajării este obligatorie.")]
        [DataType(DataType.Date)]
        [Display(Name = "Data Angajării")]
        public DateTime HireDate { get; set; } = DateTime.Now;



        [Display(Name = "Poziție Teren")]
        public string? Position { get; set; }

        [Display(Name = "Număr Tricou")]
        [Range(0, 99, ErrorMessage = "Numărul trebuie să fie între 0 și 99.")]
        public int? JerseyNumber { get; set; }

        [Display(Name = "Înălțime (cm)")]
        [Range(150, 250, ErrorMessage = "Înălțimea trebuie să fie între 150 și 250 cm.")]
        public int? Height { get; set; } 

        [Display(Name = "Greutate (kg)")]
        [Range(50, 160, ErrorMessage = "Greutatea trebuie să fie între 50 și 160 kg.")]
        public int? Weight { get; set; }


        [Display(Name = "Număr Licență")]
        public string? LicenseNumber { get; set; }


        [Display(Name = "Specializare Medicală")]
        public string? Specialization { get; set; }


        [Display(Name = "Birou / Departament")]
        public string? Office { get; set; }

    }
}
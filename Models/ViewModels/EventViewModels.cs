using System;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.ViewModels
{
    public class CreateEventViewModel
    {
        [Required(ErrorMessage = "Titlul evenimentului este obligatoriu.")]
        [Display(Name = "Nume Eveniment")]
        public string Title { get; set; }

        [Display(Name = "Locație / Descriere")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Data și ora de început sunt obligatorii.")]
        [Display(Name = "Inceput")]
        public DateTime StartTime { get; set; } = DateTime.Today.AddHours(10); 

        [Required(ErrorMessage = "Data și ora de sfârșit sunt obligatorii.")]
        [Display(Name = "Sfârșit")]
        public DateTime EndTime { get; set; } = DateTime.Today.AddHours(12); 

        [Required(ErrorMessage = "Tipul este obligatoriu.")]
        [Display(Name = "Tip Eveniment")]
        public string RelatedEntity { get; set; }
    }
}
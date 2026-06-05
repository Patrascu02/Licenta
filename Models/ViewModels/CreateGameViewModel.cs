using System;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.ViewModels
{
    public class CreateGameViewModel
    {
        [Required(ErrorMessage = "Selectați sezonul competițional.")]
        public int SeasonId { get; set; }

        [Required(ErrorMessage = "Introduceți numele echipei adverse.")]
        public string OpponentName { get; set; }

        public bool IsHomeGame { get; set; } = true; 

        [Required(ErrorMessage = "Selectați data și ora meciului.")]
        public DateTime GameDate { get; set; }

        [Required(ErrorMessage = "Introduceți locația desfășurării.")]
        public string Location { get; set; }
    }
}
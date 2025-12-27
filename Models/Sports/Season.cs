using Licenta.Models.Finance;
using Licenta.Models.Sports;

namespace Licenta.Models.Sports
{
    public class Season
    {
        public int SeasonId { get; set; }
        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Budget { get; set; }

        public ICollection<Game> Games { get; set; }
        public ICollection<Expense> Expenses { get; set; }
    }
}

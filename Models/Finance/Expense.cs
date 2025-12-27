using Licenta.Models.Sports;

namespace Licenta.Models.Finance
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        public int SeasonId { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Description { get; set; }

        public Season Season { get; set; }
    }
}

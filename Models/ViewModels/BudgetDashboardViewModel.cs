namespace Licenta.Models.ViewModels
{
    public class BudgetDashboardViewModel
    {
        public decimal BudgetLimit { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal RemainingBudget { get; set; }
        public decimal SpendingPercentage => BudgetLimit > 0 ? (TotalSpent / BudgetLimit) * 100 : 0;

       
        public decimal PlayerSalaries { get; set; }
        public decimal CoachSalaries { get; set; }
        public decimal MedicSalaries { get; set; }
        public decimal ScoutSalaries { get; set; }

        
        public Dictionary<string, decimal> OperationalExpenses { get; set; }
    }
}
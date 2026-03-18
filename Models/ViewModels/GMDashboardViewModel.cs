using Licenta.Models.Finance;
using Licenta.Models.Sports;
using Licenta.Models.Contracts;
using System.Collections.Generic;

namespace Licenta.Models.ViewModels
{
    public class GMDashboardViewModel
    {
        public Season? CurrentSeason { get; set; }

        public decimal TotalBudget { get; set; }
        public decimal MonthlySalaries { get; set; }
        public decimal SeasonalSalariesCost { get; set; }
        public decimal TotalOtherExpenses { get; set; }

        public int ActiveContractsCount { get; set; }
        public List<Expense> RecentExpenses { get; set; } = new List<Expense>();

        public List<Contract> ActiveContracts { get; set; } = new List<Contract>();

        public decimal RemainingBudget => TotalBudget - (SeasonalSalariesCost + TotalOtherExpenses);

        public double BudgetUsedPercentage => TotalBudget > 0
            ? (double)((SeasonalSalariesCost + TotalOtherExpenses) / TotalBudget) * 100
            : 0;

        public bool IsOverBudget => RemainingBudget < 0;
    }
}
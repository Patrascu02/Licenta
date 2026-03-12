using Licenta.Data;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Admin,GeneralManager")]
    public class GeneralManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GeneralManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var team = await _context.Teams.FirstOrDefaultAsync();
            decimal limit = team?.BudgetLimit ?? 1000000;

            var contracts = await _context.Contracts
                .Include(c => c.Staff).ThenInclude(s => s.Player)
                .Include(c => c.Staff).ThenInclude(s => s.Coach)
                .Include(c => c.Staff).ThenInclude(s => s.Medic)
                .Include(c => c.Staff).ThenInclude(s => s.Scout)
                .Where(c => c.IsActive)
                .ToListAsync();

            var expenses = await _context.Expenses
                .Where(e => e.ExpenseDate.Year == DateTime.Now.Year)
                .ToListAsync();

            var model = new BudgetDashboardViewModel
            {
                BudgetLimit = limit,

                PlayerSalaries = contracts.Where(c => c.Staff.Player != null).Sum(c => c.Salary),
                CoachSalaries = contracts.Where(c => c.Staff.Coach != null).Sum(c => c.Salary),
                MedicSalaries = contracts.Where(c => c.Staff.Medic != null).Sum(c => c.Salary),
                ScoutSalaries = contracts.Where(c => c.Staff.Scout != null).Sum(c => c.Salary),

                OperationalExpenses = expenses
                    .GroupBy(e => e.Type ?? "Diverse")
                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount))
            };

            decimal totalSalaries = model.PlayerSalaries + model.CoachSalaries + model.MedicSalaries + model.ScoutSalaries;
            decimal totalOps = model.OperationalExpenses.Values.Sum();

            model.TotalSpent = totalSalaries + totalOps;
            model.RemainingBudget = model.BudgetLimit - model.TotalSpent;

            return View(model);
        }


        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBudget(decimal newLimit)
        {
            
            var team = await _context.Teams.FirstOrDefaultAsync();

            if (team != null)
            {
                
                team.BudgetLimit = newLimit;
                await _context.SaveChangesAsync();
            }

            
            return RedirectToAction(nameof(Index));
        }
    }
}
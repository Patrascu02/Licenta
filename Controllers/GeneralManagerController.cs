using Licenta.Data;
using Licenta.Models.Finance;
using Licenta.Models.Sports;
using Licenta.Models.Contracts;
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

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Now;

            var currentSeason = await _context.Seasons
                .Include(s => s.Expenses)
                .FirstOrDefaultAsync(s => s.StartDate <= today && s.EndDate >= today);

            var model = new GMDashboardViewModel();

            ViewBag.AllStaff = await _context.Staff.OrderBy(s => s.LastName).ToListAsync();

            var activeContracts = await _context.Contracts
                .Include(c => c.Staff)
                .Where(c => c.IsActive)
                .ToListAsync();

            model.ActiveContracts = activeContracts;

            if (currentSeason != null)
            {
                decimal monthlySalaries = activeContracts.Sum(c => c.Salary);
                decimal seasonalSalariesCost = monthlySalaries * 10;
                decimal otherExpenses = currentSeason.Expenses?.Sum(e => e.Amount) ?? 0;

                model.CurrentSeason = currentSeason;
                model.TotalBudget = currentSeason.Budget;
                model.MonthlySalaries = monthlySalaries;
                model.SeasonalSalariesCost = seasonalSalariesCost;
                model.TotalOtherExpenses = otherExpenses;
                model.ActiveContractsCount = activeContracts.Count;
                model.RecentExpenses = currentSeason.Expenses?.OrderByDescending(e => e.ExpenseDate).Take(5).ToList() ?? new();
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> StartNewSeason(decimal totalBudget)
        {
            var today = DateTime.Now;
            int startYear = today.Month >= 9 ? today.Year : today.Year - 1;

            var newSeason = new Season
            {
                Name = $"Sezonul {startYear} - {startYear + 1}",
                StartDate = new DateTime(startYear, 9, 1),
                EndDate = new DateTime(startYear + 1, 8, 31),
                Budget = totalBudget
            };

            _context.Seasons.Add(newSeason);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddExpense(int seasonId, string type, string description, decimal amount)
        {
            var expense = new Expense
            {
                SeasonId = seasonId,
                Type = type,
                Description = description ?? "",
                Amount = amount,
                ExpenseDate = DateTime.Now
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteExpense(int expenseId)
        {
            var expense = await _context.Expenses.FindAsync(expenseId);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AssignContract(int staffId, decimal salary, DateTime startDate, IFormFile? contractFile)
        {
            var oldContracts = await _context.Contracts.Where(c => c.StaffId == staffId).ToListAsync();
            foreach (var c in oldContracts)
            {
                c.IsActive = false;
            }

            string? savedFilePath = null;

            if (contractFile != null && contractFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "contracts");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(contractFile.FileName);
                var exactPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(exactPath, FileMode.Create))
                {
                    await contractFile.CopyToAsync(stream);
                }

                savedFilePath = "/uploads/contracts/" + uniqueFileName;
            }

            DateTime calculatedEndDate;
            if (startDate.Month >= 9) 
            {
                calculatedEndDate = new DateTime(startDate.Year + 1, 8, 31);
            }
            else 
            {
                calculatedEndDate = new DateTime(startDate.Year, 8, 31);
            }

            var newContract = new Contract
            {
                StaffId = staffId,
                Salary = salary,
                StartDate = startDate,
                EndDate = calculatedEndDate, 
                IsActive = true,
                SignedFilePath = savedFilePath
            };

            _context.Contracts.Add(newContract);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContract(int contractId)
        {
            var contract = await _context.Contracts.FindAsync(contractId);
            if (contract != null)
            {
                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
using Licenta.Data;
using Licenta.Models.Medical;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Medic,Admin")]
    public class MedicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var roster = await _context.Players
                .Include(p => p.Staff)
                .ToListAsync();

            var activeInjuries = await _context.Injuries
                .Where(i => i.Status != "Recuperat")
                .ToListAsync();

            var model = new MedicDashboardViewModel();

            foreach (var player in roster)
            {
                var pStatus = new PlayerMedicalStatus
                {
                    PlayerId = player.PlayerId,
                    FullName = $"{player.Staff.FirstName} {player.Staff.LastName}",
                    JerseyNumber = player.JerseyNumber.ToString(),
                    LastClearance = player.LastMedicalClearance,
                    ActiveInjury = activeInjuries.FirstOrDefault(i => i.PlayerId == player.PlayerId)
                };

                if (!pStatus.IsClearanceValid) model.ExpiredClearances++;
                if (pStatus.ActiveInjury != null) model.TotalInjured++;

                model.Players.Add(pStatus);
            }

            return View(model);
        }

        // --- ACORDARE VIZĂ MEDICALĂ (OK DE JOC) ---
        [HttpPost]
        public async Task<IActionResult> GrantClearance(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player != null)
            {
                player.LastMedicalClearance = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // --- RAPORT ACCIDENTARE ---
        [HttpPost]
        public async Task<IActionResult> ReportInjury(int playerId, string description, string recoveryText)
        {
            // Lipim textul de recuperare de diagnostic, dacă medicul l-a completat
            string finalDescription = string.IsNullOrWhiteSpace(recoveryText)
                ? description
                : $"{description} (Timp estimat recuperare: {recoveryText})";

            var injury = new Injury
            {
                PlayerId = playerId,
                Description = finalDescription,
                StartDate = DateTime.Now,
                EstimatedRecoveryDate = DateTime.Now, // Punem o dată default ca să nu dea eroare SQL-ul
                Status = "Accidentat"
            };

            _context.Injuries.Add(injury);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        // --- DECLARARE RECUPERAT ---
        [HttpPost]
        public async Task<IActionResult> MarkRecovered(int injuryId)
        {
            var injury = await _context.Injuries.FindAsync(injuryId);
            if (injury != null)
            {
                injury.Status = "Recuperat";
                // Am eliminat EndDate pentru a nu mai da eroare
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
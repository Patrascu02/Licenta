using Licenta.Data;
using Licenta.Models.Sports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize]
    public class ScoutController : Controller // Numele schimbat din Scouting în Scout
    {
        private readonly ApplicationDbContext _context;

        public ScoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- GET: AFISARE FORMULAR INTRODUCERE MEDII LUNARE ---
        [HttpGet]
        public async Task<IActionResult> AddMonthlyStats(int playerId)
        {
            // Verificăm permisiunea de Scouting prin sistemul ACL implementat anterior
            if (!User.HasClaim("Permission", "Scouting.Manage"))
            {
                return Forbid();
            }

            var player = await _context.Players
                .Include(p => p.Staff)
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null) return NotFound();

            // Pregătim modelul cu valori implicite pentru luna și anul curent
            var model = new PlayerGameStats
            {
                PlayerId = playerId,
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
                IsScoutingReport = true // Marcăm automat ca raport de scouting
            };

            ViewBag.PlayerName = $"{player.Staff.FirstName} {player.Staff.LastName}";
            return View(model);
        }

        // --- POST: SALVARE MEDII LUNARE ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMonthlyStats(PlayerGameStats stats)
        {
            if (!User.HasClaim("Permission", "Scouting.Manage")) return Forbid();

            // Forțăm marcarea ca raport de scouting pentru a evita erorile de GameId nullable
            stats.IsScoutingReport = true;
            stats.GameId = null;

            if (ModelState.IsValid)
            {
                // Verificăm dacă scouterul a introdus deja date pentru această lună/an la acest jucător
                var existingReport = await _context.PlayerGameStats
                    .AnyAsync(s => s.PlayerId == stats.PlayerId &&
                                   s.Month == stats.Month &&
                                   s.Year == stats.Year &&
                                   s.IsScoutingReport);

                if (existingReport)
                {
                    ModelState.AddModelError("", "Există deja un raport de scouting pentru această lună.");
                    return View(stats);
                }

                _context.PlayerGameStats.Add(stats);
                await _context.SaveChangesAsync();

                // Redirect înapoi la profilul jucătorului
                return RedirectToAction("Details", "Players", new { id = stats.PlayerId });
            }

            return View(stats);
        }
    }
}
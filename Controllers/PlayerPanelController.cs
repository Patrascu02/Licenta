using Licenta.Data;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    // Doar cei cu rolul de 'Player' au acces aici
    [Authorize(Roles = "Player")]
    public class PlayerPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        // REPARAT: Folosim IdentityUser în loc de ApplicationUser
        private readonly UserManager<IdentityUser> _userManager;

        public PlayerPanelController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            // 1. Găsim Jucătorul Logat
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User-ul nu a fost găsit.");

            var staff = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Contracts)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (staff == null || staff.Player == null)
                return NotFound("Profilul de jucător nu a putut fi identificat.");

            var player = staff.Player;

            // 2. Extragem echipa actuală
            var team = await _context.Teams.FirstOrDefaultAsync(t => t.TeamId == player.CurrentTeamId);

            // 3. Extragem Statisticile (AICI ERA EROAREA)
            var allStats = await _context.PlayerGameStats
                .Include(s => s.Game)
                .Where(s => s.PlayerId == player.PlayerId)
                .ToListAsync();

            // Filtrăm doar statisticile care au un meci valid atașat pentru tabelul de Performanțe Recente
            var recentPerformances = allStats
                .Where(s => s.Game != null) // <-- FIX: Evităm eroarea "Game.get returned null"
                .OrderByDescending(s => s.Game.GameDate)
                .Take(5)
                .ToList();

            // Calculăm mediile din TOATE statisticile (chiar și cele fără meci specific)
            double avgPts = allStats.Any() ? allStats.Average(s => s.Points) : 0;
            double avgReb = allStats.Any() ? allStats.Average(s => s.Rebounds) : 0;
            double avgAst = allStats.Any() ? allStats.Average(s => s.Assists) : 0;

            // 4. Contractul Activ
            var activeContract = staff.Contracts?
                .FirstOrDefault(c => c.StartDate <= DateTime.Now &&
                                    (c.EndDate == null || c.EndDate >= DateTime.Now) &&
                                    c.IsActive);

            // 5. Accidentări active
            var injuries = await _context.Injuries
                .Where(i => i.PlayerId == player.PlayerId && i.Status != "Recuperat")
                .ToListAsync();

            // 6. Meciuri viitoare
            var upcomingGames = await _context.Games
                .Where(g => g.GameDate >= DateTime.Now)
                .OrderBy(g => g.GameDate)
                .Take(3)
                .ToListAsync();

            // 7. Populăm ViewModel-ul
            var model = new PlayerDashboardViewModel
            {
                StaffInfo = staff,
                PlayerInfo = player,
                CurrentTeam = team,
                ActiveContract = activeContract,
                AvgPoints = Math.Round(avgPts, 1),
                AvgRebounds = Math.Round(avgReb, 1),
                AvgAssists = Math.Round(avgAst, 1),
                GamesPlayed = allStats.Count,
                RecentPerformances = recentPerformances,
                UpcomingGames = upcomingGames,
                ActiveInjuries = injuries
            };

            return View(model);
        }
    }
}
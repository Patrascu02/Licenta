using Licenta.Data;
using Licenta.Models.Identity;
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
    [Authorize(Roles = "Player")]
    public class PlayerPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PlayerPanelController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

            // 3. Extragem Statisticile (Performanțe recente) - CORECTAT la .GameDate
            var allStats = await _context.PlayerGameStats
                .Include(s => s.Game)
                .Where(s => s.PlayerId == player.PlayerId)
                .OrderByDescending(s => s.Game.GameDate)
                .ToListAsync();

            var recentPerformances = allStats.Take(5).ToList();

            // Calculăm mediile
            double avgPts = allStats.Any() ? allStats.Average(s => s.Points) : 0;
            double avgReb = allStats.Any() ? allStats.Average(s => s.Rebounds) : 0;
            double avgAst = allStats.Any() ? allStats.Average(s => s.Assists) : 0;

            // 4. Contractul Activ (Verificăm dacă EndDate e null sau în viitor)
            var activeContract = staff.Contracts?
                .FirstOrDefault(c => c.StartDate <= DateTime.Now &&
                                    (c.EndDate == null || c.EndDate >= DateTime.Now) &&
                                    c.IsActive);

            // 5. Accidentări active - CORECTAT la folosirea lui Status
            var injuries = await _context.Injuries
                .Where(i => i.PlayerId == player.PlayerId && i.Status != "Recuperat") // Asigură-te că folosești stringuri potrivite aici
                .ToListAsync();

            // 6. Meciuri viitoare - CORECTAT la .GameDate
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
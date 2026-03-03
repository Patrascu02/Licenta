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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User-ul nu a fost găsit.");

            var staff = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Contracts)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (staff == null || staff.Player == null)
                return NotFound("Profilul de jucător nu a putut fi identificat.");

            var player = staff.Player;

            var team = await _context.Teams.FirstOrDefaultAsync(t => t.TeamId == player.CurrentTeamId);

            var allStats = await _context.PlayerGameStats
                .Include(s => s.Game)
                .Where(s => s.PlayerId == player.PlayerId)
                .ToListAsync();

            var recentPerformances = allStats
                .Where(s => s.Game != null)
                .OrderByDescending(s => s.Game.GameDate)
                .Take(5)
                .ToList();

            double avgPts = allStats.Any() ? allStats.Average(s => s.Points) : 0;
            double avgReb = allStats.Any() ? allStats.Average(s => s.Rebounds) : 0;
            double avgAst = allStats.Any() ? allStats.Average(s => s.Assists) : 0;

            var activeContract = staff.Contracts?
                .FirstOrDefault(c => c.StartDate <= DateTime.Now &&
                                    (c.EndDate == null || c.EndDate >= DateTime.Now) &&
                                    c.IsActive);

            var injuries = await _context.Injuries
                .Where(i => i.PlayerId == player.PlayerId && i.Status != "Recuperat")
                .ToListAsync();

            var upcomingGames = await _context.Games
                .Where(g => g.GameDate >= DateTime.Now)
                .OrderBy(g => g.GameDate)
                .Take(3)
                .ToListAsync();

            var nextEvent = await _context.Events
                .Where(e => e.StartTime >= DateTime.Now)
                .OrderBy(e => e.StartTime)
                .FirstOrDefaultAsync();

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
                ActiveInjuries = injuries,
                NextEvent = nextEvent
            };

            // --- CALCUL PALMARES ECHIPĂ PENTRU DASHBOARD ---
            var pastGamesForRecord = await _context.Games
                .Where(g => g.GameDate < DateTime.Now && (g.HomeScore > 0 || g.AwayScore > 0))
                .ToListAsync();

            int wins = 0; int losses = 0;
            foreach (var g in pastGamesForRecord)
            {
                bool isHome = g.Location.ToLower().Contains("acas");
                if (isHome && g.HomeScore > g.AwayScore) wins++;
                else if (!isHome && g.AwayScore > g.HomeScore) wins++;
                else losses++;
            }
            ViewBag.Wins = wins;
            ViewBag.Losses = losses;
            // -----------------------------------------------

            return View(model);
        }
    }
}
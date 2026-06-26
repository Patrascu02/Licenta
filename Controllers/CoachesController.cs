using Licenta.Data;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize]
    public class CoachesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CoachesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!User.IsInRole("Admin") && !User.HasClaim("Permission", "Coaches.View"))
            {
                return Redirect("/Identity/Account/AccessDenied");
            }

            var coaches = await _context.Coaches
                .Include(c => c.Staff)
                .ToListAsync();

            return View(coaches);
        }


        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User-ul nu a fost găsit.");

            var staff = await _context.Staff
                .Include(s => s.Coach)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (staff == null || staff.Coach == null)
                return NotFound("Profilul de antrenor nu a putut fi identificat.");

            int totalPlayers = await _context.Players.CountAsync();
            int injuredCount = await _context.Injuries.CountAsync(i => i.Status != "Recuperat");

            var upcomingGames = await _context.Games
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Where(g => g.GameDate >= DateTime.Now)
                .OrderBy(g => g.GameDate)
                .Take(4)
                .ToListAsync();

            var recentGames = await _context.Games
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Where(g => g.GameDate < DateTime.Now)
                .OrderByDescending(g => g.GameDate)
                .Take(5)
                .ToListAsync();

            var nextEvent = await _context.Events
                .Where(e => e.StartTime >= DateTime.Now)
                .OrderBy(e => e.StartTime)
                .FirstOrDefaultAsync();

            var model = new CoachDashboardViewModel
            {
                StaffInfo = staff,
                CoachInfo = staff.Coach,
                UpcomingGames = upcomingGames,
                RecentGames = recentGames,
                TotalPlayers = totalPlayers,
                InjuredPlayersCount = injuredCount,
                NextEvent = nextEvent
            };

            var mainTeam = await _context.Teams.FirstOrDefaultAsync(t => t.Name.Contains("STEAUA") || t.TeamId == 2);
            int mainTeamId = mainTeam?.TeamId ?? 2;

            var pastGamesForRecord = await _context.Games
                .Where(g => g.GameDate < DateTime.Now && (g.HomeScore > 0 || g.AwayScore > 0))
                .ToListAsync();

            int wins = 0; int losses = 0;
            foreach (var g in pastGamesForRecord)
            {
                if (g.HomeTeamId == mainTeamId)
                {
                    if (g.HomeScore > g.AwayScore) wins++;
                    else if (g.HomeScore < g.AwayScore) losses++;
                }
                else if (g.AwayTeamId == mainTeamId)
                {
                    if (g.AwayScore > g.HomeScore) wins++;
                    else if (g.AwayScore < g.HomeScore) losses++;
                }
            }
            ViewBag.Wins = wins;
            ViewBag.Losses = losses;

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> Calendar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == user.Id);

            var events = await _context.Events
                .Where(e => e.StaffId == staff.StaffId)
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            return View(events);
        }

        [HttpGet]
        [Authorize(Roles = "Coach")]
        public IActionResult CreateEvent()
        {
            return View(new CreateEventViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Coach")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model)
        {
            if (model.StartTime >= model.EndTime)
            {
                ModelState.AddModelError("EndTime", "Ora de sfârșit trebuie să fie după ora de început.");
            }

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == user.Id);

            string? savedFilePath = null;

            if (model.AttachedFile != null && model.AttachedFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "events");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.AttachedFile.FileName);
                var exactPath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(exactPath, FileMode.Create))
                {
                    await model.AttachedFile.CopyToAsync(stream);
                }

                savedFilePath = "/uploads/events/" + uniqueFileName;
            }

            var newEvent = new Licenta.Models.Calendar.Event
            {
                Title = model.Title,
                Description = model.Description,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                StaffId = staff.StaffId,
                RelatedEntity = model.RelatedEntity,
                AttachedFilePath = savedFilePath
            };

            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Calendar));
        }

        [HttpPost]
        [Authorize(Roles = "Coach")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev != null)
            {
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Calendar));
        }

        [HttpGet]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> TacticalReports()
        {
            var pastGames = await _context.Games
                .Include(g => g.HomeTeam)
                .Include(g => g.AwayTeam)
                .Where(g => g.GameDate < DateTime.Now)
                .OrderByDescending(g => g.GameDate)
                .ToListAsync();

            var mainTeam = await _context.Teams.FirstOrDefaultAsync(t => t.Name.Contains("STEAUA") || t.TeamId == 2);
            int mainTeamId = mainTeam?.TeamId ?? 2;

            int wins = 0; int losses = 0;
            foreach (var g in pastGames.Where(g => g.HomeScore > 0 || g.AwayScore > 0))
            {
                if (g.HomeTeamId == mainTeamId)
                {
                    if (g.HomeScore > g.AwayScore) wins++;
                    else if (g.HomeScore < g.AwayScore) losses++;
                }
                else if (g.AwayTeamId == mainTeamId)
                {
                    if (g.AwayScore > g.HomeScore) wins++;
                    else if (g.AwayScore < g.HomeScore) losses++;
                }
            }
            ViewBag.Wins = wins;
            ViewBag.Losses = losses;

            return View(pastGames);
        }

        [HttpGet]
        [Authorize(Roles = "Coach")]
        public async Task<IActionResult> ViewGameStats(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null) return NotFound();

            var model = new Licenta.Models.ViewModels.EditGameStatsViewModel
            {
                GameId = game.GameId,
                GameInfo = $"{game.GameDate:dd MMM yyyy} | {game.Location}",
                HomeScore = game.HomeScore,
                AwayScore = game.AwayScore
            };

            var roster = await _context.Players.Include(p => p.Staff).ToListAsync();
            var existingStats = await _context.PlayerGameStats.Where(s => s.GameId == id && !s.IsScoutingReport).ToListAsync();

            foreach (var player in roster)
            {
                var stat = existingStats.FirstOrDefault(s => s.PlayerId == player.PlayerId);
                model.PlayerStats.Add(new Licenta.Models.ViewModels.PlayerStatItemViewModel
                {
                    PlayerId = player.PlayerId,
                    PlayerName = $"{player.Staff.FirstName} {player.Staff.LastName}",
                    Position = player.Position ?? "-",
                    JerseyNumber = player.JerseyNumber,
                    Points = stat?.Points ?? 0,
                    Rebounds = stat?.Rebounds ?? 0,
                    Assists = stat?.Assists ?? 0,
                    Steals = stat?.Steals ?? 0,
                    Blocks = stat?.Blocks ?? 0,
                    MinutesPlayed = stat?.MinutesPlayed ?? 0
                });
            }

            model.PlayerStats = model.PlayerStats.OrderBy(p => p.JerseyNumber).ToList();
            return View(model);
        }
    }
}
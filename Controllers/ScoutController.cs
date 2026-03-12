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
    public class ScoutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScoutController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        //         PANOU PRINCIPAL (DASHBOARD)
        // ==========================================

        [HttpGet]
        [Authorize(Roles = "Scout")]
        public async Task<IActionResult> Dashboard()
        {
            var pastGames = await _context.Games
                .Where(g => g.GameDate < DateTime.Now)
                .OrderByDescending(g => g.GameDate)
                .ToListAsync();

            var pendingGames = pastGames.Where(g => g.HomeScore == 0 && g.AwayScore == 0).ToList();
            var completedGames = pastGames.Where(g => g.HomeScore > 0 || g.AwayScore > 0).ToList();

            ViewBag.PendingCount = pendingGames.Count;
            ViewBag.CompletedCount = completedGames.Count;

            ViewBag.RecentPending = pendingGames.Take(3).ToList();

            // --- CALCUL PALMARES ECHIPĂ PENTRU DASHBOARD ---
            int wins = 0; int losses = 0;
            foreach (var g in completedGames)
            {
                bool isHome = g.Location.ToLower().Contains("acas");
                if (isHome && g.HomeScore > g.AwayScore) wins++;
                else if (!isHome && g.AwayScore > g.HomeScore) wins++;
                else losses++;
            }

            ViewBag.Wins = wins;
            ViewBag.Losses = losses;

            return View();
        }

        // --- GET: AFISARE FORMULAR INTRODUCERE MEDII LUNARE ---
        [HttpGet]
        public async Task<IActionResult> AddMonthlyStats(int playerId)
        {
            if (!User.HasClaim("Permission", "Scouting.Manage"))
            {
                return Forbid();
            }

            var player = await _context.Players
                .Include(p => p.Staff)
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);

            if (player == null) return NotFound();

            var model = new PlayerGameStats
            {
                PlayerId = playerId,
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
                IsScoutingReport = true
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

            stats.IsScoutingReport = true;
            stats.GameId = null;

            if (ModelState.IsValid)
            {
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

                return RedirectToAction("Details", "Players", new { id = stats.PlayerId });
            }

            return View(stats);
        }

        // ==========================================
        //         MODUL RAPOARTE MECIURI (SCOUT)
        // ==========================================

        [HttpGet]
        [Authorize(Roles = "Scout")]
        public async Task<IActionResult> MatchReports(string status = "all")
        {
            var allPastGames = await _context.Games
                .Where(g => g.GameDate < DateTime.Now)
                .OrderByDescending(g => g.GameDate)
                .ToListAsync();

            int wins = 0; int losses = 0;
            foreach (var g in allPastGames.Where(g => g.HomeScore > 0 || g.AwayScore > 0))
            {
                bool isHome = g.Location.ToLower().Contains("acas");
                if (isHome && g.HomeScore > g.AwayScore) wins++;
                else if (!isHome && g.AwayScore > g.HomeScore) wins++;
                else losses++;
            }
            ViewBag.Wins = wins;
            ViewBag.Losses = losses;
            ViewBag.CurrentStatus = status; 

            var filteredGames = allPastGames.AsEnumerable();
            if (status == "pending")
            {
                filteredGames = filteredGames.Where(g => g.HomeScore == 0 && g.AwayScore == 0);
            }
            else if (status == "completed")
            {
                filteredGames = filteredGames.Where(g => g.HomeScore > 0 || g.AwayScore > 0);
            }

            return View(filteredGames.ToList());
        }

        [HttpGet]
        [Authorize(Roles = "Scout")]
        public async Task<IActionResult> EditMatchStats(int id)
        {
            var game = await _context.Games.FindAsync(id);
            if (game == null || game.GameDate >= DateTime.Now) return NotFound();

            var model = new Licenta.Models.ViewModels.EditGameStatsViewModel
            {
                GameId = game.GameId,
                GameInfo = $"{game.GameDate:dd MMM yyyy} | {game.Location} | {game.GameType}",
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
                    PlayerGameStatsId = stat?.PlayerGameStatsId ?? 0,
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

        [HttpPost]
        [Authorize(Roles = "Scout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMatchStats(Licenta.Models.ViewModels.EditGameStatsViewModel model)
        {
            var game = await _context.Games.FindAsync(model.GameId);
            if (game == null) return NotFound();

            ModelState.Remove("GameInfo");
            for (int i = 0; i < model.PlayerStats.Count; i++)
            {
                ModelState.Remove($"PlayerStats[{i}].PlayerName");
                ModelState.Remove($"PlayerStats[{i}].Position");
            }

            if (model.HomeScore == model.AwayScore && model.HomeScore != 0)
            {
                ModelState.AddModelError("", "Eroare: În baschet nu există rezultat de egalitate (meciul intră în prelungiri).");
            }

            bool isHome = game.Location.ToLower().Contains("acas");
            int ourTeamScore = isHome ? model.HomeScore : model.AwayScore;

            int sumPoints = (int)model.PlayerStats.Sum(p => p.Points);

            if (sumPoints != ourTeamScore)
            {
                ModelState.AddModelError("", $"Atenție: Scorul echipei noastre este setat la {ourTeamScore} puncte, dar punctele jucătorilor adunate dau {sumPoints}! Aceste două numere trebuie să fie EXACT egale pentru a salva raportul.");
            }

            if (!ModelState.IsValid)
            {
                model.GameInfo = $"{game.GameDate:dd MMM yyyy} | {game.Location} | {game.GameType}";

                var roster = await _context.Players.Include(p => p.Staff).ToListAsync();
                foreach (var p in model.PlayerStats)
                {
                    var playerDb = roster.FirstOrDefault(x => x.PlayerId == p.PlayerId);
                    if (playerDb != null)
                    {
                        p.PlayerName = $"{playerDb.Staff.FirstName} {playerDb.Staff.LastName}";
                        p.Position = playerDb.Position ?? "-";
                    }
                }

                return View(model);
            }

            game.HomeScore = model.HomeScore;
            game.AwayScore = model.AwayScore;

            foreach (var ps in model.PlayerStats)
            {
                if (ps.MinutesPlayed == 0 && ps.PlayerGameStatsId == 0) continue;

                if (ps.PlayerGameStatsId == 0)
                {
                    _context.PlayerGameStats.Add(new PlayerGameStats
                    {
                        GameId = model.GameId,
                        PlayerId = ps.PlayerId,
                        Points = ps.Points,
                        Rebounds = ps.Rebounds,
                        Assists = ps.Assists,
                        Steals = ps.Steals,
                        Blocks = ps.Blocks,
                        MinutesPlayed = ps.MinutesPlayed,
                        IsScoutingReport = false,
                        Month = game.GameDate.Month,
                        Year = game.GameDate.Year
                    });
                }
                else
                {
                    var existing = await _context.PlayerGameStats.FindAsync(ps.PlayerGameStatsId);
                    if (existing != null)
                    {
                        existing.Points = ps.Points; existing.Rebounds = ps.Rebounds; existing.Assists = ps.Assists;
                        existing.Steals = ps.Steals; existing.Blocks = ps.Blocks; existing.MinutesPlayed = ps.MinutesPlayed;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MatchReports));
        }
    }
}
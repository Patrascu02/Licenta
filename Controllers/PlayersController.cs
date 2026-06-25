using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Licenta.Data;
using Licenta.Models.Roles;
using Licenta.Models.Core;
using Licenta.Models.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Licenta.Controllers
{
    [Authorize]
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PlayersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var players = await _context.Players
                .Include(p => p.Staff).ThenInclude(s => s.Contracts)
                .Include(p => p.Injuries)
                .ToListAsync();

            var playerStatuses = new Dictionary<int, string>();
            foreach (var player in players)
            {
                bool hasActiveContract = player.Staff?.Contracts != null &&
                                         player.Staff.Contracts.Any(c => c.IsActive && (c.EndDate == null || c.EndDate >= DateTime.Now));

                bool hasValidMedicalVisa = player.LastMedicalClearance.HasValue &&
                                           player.LastMedicalClearance.Value.AddMonths(8) >= DateTime.Now;

                bool isInjured = player.Injuries != null &&
                                 player.Injuries.Any(i => i.Status != "Recuperat");

                if (!hasActiveContract)
                    playerStatuses[player.PlayerId] = "FĂRĂ CONTRACT";
                else if (!hasValidMedicalVisa)
                    playerStatuses[player.PlayerId] = "LIPSĂ VIZĂ MEDICALĂ";
                else if (isInjured)
                    playerStatuses[player.PlayerId] = "INDISPONIBIL (ACCIDENTAT)";
                else
                    playerStatuses[player.PlayerId] = "APT DE JOC";
            }

            ViewBag.PlayerStatuses = playerStatuses;
            return View(players);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var player = await _context.Players
                .Include(p => p.Staff).ThenInclude(s => s.Contracts)
                .Include(p => p.GameStats)
                .Include(p => p.Injuries)
                .FirstOrDefaultAsync(m => m.PlayerId == id);

            if (player == null) return NotFound();

            bool hasActiveContract = player.Staff?.Contracts != null &&
                                     player.Staff.Contracts.Any(c => c.IsActive && (c.EndDate == null || c.EndDate >= DateTime.Now));

            bool hasValidMedicalVisa = player.LastMedicalClearance.HasValue &&
                                       player.LastMedicalClearance.Value.AddMonths(8) >= DateTime.Now;

            bool isInjured = player.Injuries != null &&
                             player.Injuries.Any(i => i.Status != "Recuperat");

            string statusCurent = "";
            if (!hasActiveContract) statusCurent = "FĂRĂ CONTRACT";
            else if (!hasValidMedicalVisa) statusCurent = "LIPSĂ VIZĂ MEDICALĂ";
            else if (isInjured) statusCurent = "INDISPONIBIL (ACCIDENTAT)";
            else statusCurent = "APT DE JOC";

            ViewBag.PlayerStatus = statusCurent;

            return View(player);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!User.HasClaim("Permission", "Players.Create") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Player player)
        {
            if (!User.HasClaim("Permission", "Players.Create") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(player);
                await _context.SaveChangesAsync();
                await LogAuditAction($"A creat un jucător nou (ID: {player.PlayerId})", "Player", player.PlayerId);
                return RedirectToAction(nameof(Index));
            }

            return View(player);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            if (!User.HasClaim("Permission", "Players.Edit") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var player = await _context.Players.Include(p => p.Staff).FirstOrDefaultAsync(p => p.PlayerId == id);
            if (player == null) return NotFound();

            return View(player);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PlayerId,StaffId,JerseyNumber,Height,Weight,Position")] Player player)
        {
            if (id != player.PlayerId) return NotFound();

            if (!User.HasClaim("Permission", "Players.Edit") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            ModelState.Remove("Staff");

            if (ModelState.IsValid)
            {
                try
                {
                    var playerToUpdate = await _context.Players.FirstOrDefaultAsync(p => p.PlayerId == id);

                    if (playerToUpdate == null) return NotFound();

                    playerToUpdate.JerseyNumber = player.JerseyNumber;
                    playerToUpdate.Height = player.Height;
                    playerToUpdate.Weight = player.Weight;
                    playerToUpdate.Position = player.Position;

                    await _context.SaveChangesAsync();

                    await LogAuditAction($"A modificat datele jucătorului (ID: {player.PlayerId})", "Player", player.PlayerId);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PlayerExists(player.PlayerId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(player);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            if (!User.HasClaim("Permission", "Players.Delete") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var player = await _context.Players.Include(p => p.Staff).FirstOrDefaultAsync(m => m.PlayerId == id);
            if (player == null) return NotFound();

            return View(player);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!User.HasClaim("Permission", "Players.Delete") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var player = await _context.Players
                .Include(p => p.Staff)
                .FirstOrDefaultAsync(p => p.PlayerId == id);

            if (player != null)
            {
                var name = player.Staff != null ? $"{player.Staff.FirstName} {player.Staff.LastName}" : "Jucător Necunoscut";

                var staff = player.Staff;
                string userId = staff?.UserId;

                _context.Players.Remove(player);

                if (staff != null)
                {
                    _context.Staff.Remove(staff);
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }

                await LogAuditAction($"A șters definitiv profilul și contul de utilizator pentru: {name}", "User Account", id);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PlayerExists(int id) => _context.Players.Any(e => e.PlayerId == id);

        private async Task LogAuditAction(string action, string entityName, int entityId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var staffMember = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            if (staffMember != null)
            {
                _context.AuditLogs.Add(new AuditLog { Action = action, Timestamp = DateTime.Now, StaffId = staffMember.StaffId, EntityName = entityName, EntityId = entityId });
            }
        }
    }
}
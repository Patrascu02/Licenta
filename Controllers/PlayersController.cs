using Licenta.Data;
using Licenta.Models.Roles; // Pentru Player
using Licenta.Models.Core;  // Pentru Staff
using Licenta.Models.Security; // Pentru AuditLog
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize] // Trebuie să fii logat pentru a accesa oricare dintre aceste metode
    public class PlayersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PlayersController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- 1. LISTA JUCĂTORI (READ) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // VERIFICARE PERMISIUNE: Are dreptul să vadă jucătorii?
            if (!User.HasClaim("Permission", "Players.View"))
            {
                return RedirectToAction("AccessDenied", "Account"); // Sau return Forbid();
            }

            var players = await _context.Players
                .Include(p => p.Staff) // Aducem și numele (din Staff)
                .ToListAsync();

            return View(players);
        }

        // --- 2. DETALII JUCĂTOR (READ) ---
        // În PlayersController.cs
        public async Task<IActionResult> Details(int id)
        {
            // 1. Găsim jucătorul cerut
            var player = await _context.Players
                .Include(p => p.Staff)
                    .ThenInclude(s => s.Contracts) 
                .Include(p => p.CurrentTeam)
                .Include(p => p.GameStats)
                .FirstOrDefaultAsync(m => m.PlayerId == id);

            if (player == null) return NotFound();

            // 2. VERIFICARE PERMISIUNI (Actualizată)
            // Acces permis dacă:
            // a) Ești Admin/Antrenor (Players.View)
            // b) Ești Scouter (Scouting.Manage)
            // c) Ești chiar jucătorul respectiv (Self-Service)

            var currentUserId = _userManager.GetUserId(User);
            var isOwnProfile = (player.Staff.UserId == currentUserId);
            // Notă: Asigură-te că Staff are UserId populat corect

            bool hasAdminRights = User.HasClaim(c => c.Type == "Permission" &&
                                  (c.Value == "Players.View" || c.Value == "Scouting.Manage"));

            if (!hasAdminRights && !isOwnProfile)
            {
                return Forbid(); // Nu ai voie să vezi profilul altui coleg
            }

            return View(player);
        }

        // --- 3. EDITARE JUCĂTOR (UPDATE - GET) ---
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // VERIFICARE CRITICĂ: Are dreptul să modifice?
            if (!User.HasClaim("Permission", "Players.Edit"))
            {
                // Îl trimitem la o pagină de eroare sau îi tăiem accesul
                return Forbid();
            }

            var player = await _context.Players
                .Include(p => p.Staff)
                .FirstOrDefaultAsync(p => p.PlayerId == id);

            if (player == null) return NotFound();

            return View(player);
        }

        // --- 4. EDITARE JUCĂTOR (UPDATE - POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Player player)
        {
            if (!User.HasClaim("Permission", "Players.Edit")) return Forbid();

            if (id != player.PlayerId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Atașăm entitatea pentru a nu suprascrie tot (doar câmpurile modificate)
                    _context.Update(player);
                    await _context.SaveChangesAsync();

                    // AUDIT: Înregistrăm cine a făcut modificarea
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

       
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!User.HasClaim("Permission", "Players.Delete")) return Forbid();

            var player = await _context.Players.Include(p => p.Staff).FirstOrDefaultAsync(p => p.PlayerId == id);
            if (player != null)
            {
                var name = $"{player.Staff.FirstName} {player.Staff.LastName}";
                _context.Players.Remove(player);

                await LogAuditAction($"A șters profilul de jucător pentru: {name}", "Player", id);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

     
        private bool PlayerExists(int id)
        {
            return _context.Players.Any(e => e.PlayerId == id);
        }

        private async Task LogAuditAction(string action, string entityName, int entityId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var staffMember = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (staffMember != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = action,
                    Timestamp = DateTime.Now,
                    StaffId = staffMember.StaffId,
                    EntityName = entityName,
                    EntityId = entityId
                });
            }
        }


        // --- 6. PROFILUL MEU (Pentru Jucătorii Logați) ---
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            // 1. Aflăm ID-ul userului conectat
            var userId = _userManager.GetUserId(User);

            // 2. Căutăm în tabela Staff -> apoi legătura cu Player
            var staffMember = await _context.Staff
                .Include(s => s.Player) // Încărcăm și datele de jucător
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (staffMember == null || staffMember.Player == null)
            {
                // Dacă userul e logat dar nu e legat de un jucător (ex: e doar Admin)
                return RedirectToAction("Index", "Home");
            }

            // 3. Redirecționăm către metoda Details folosind ID-ul corect
            // Astfel refolosim pagina Details.cshtml pe care ai reparat-o deja
            return RedirectToAction("Details", new { id = staffMember.Player.PlayerId });
        }
    }
}
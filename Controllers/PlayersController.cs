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

        // --- 1. LISTA JUCĂTORI (READ) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // IMPORTANT: Aici încărcăm Staff (Nume) și CurrentTeam (Echipa)
            // Fără .Include(p => p.CurrentTeam), numele echipei va fi gol în tabel!
            var players = _context.Players
                .Include(p => p.Staff)
                .Include(p => p.CurrentTeam);

            return View(await players.ToListAsync());
        }

        // --- 2. DETALII JUCĂTOR (READ) ---
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var player = await _context.Players
                .Include(p => p.Staff)
                    .ThenInclude(s => s.Contracts)
                .Include(p => p.CurrentTeam)
                .Include(p => p.GameStats)
                .FirstOrDefaultAsync(m => m.PlayerId == id);

            if (player == null) return NotFound();

            // LOGICA DE ACCES:
            // 1. Ești Admin/GM/Coach? Ai voie.
            // 2. Ești chiar tu (jucătorul)? Ai voie.

            var currentUserId = _userManager.GetUserId(User);
            bool isOwnProfile = (player.Staff.UserId == currentUserId);
            bool hasManagementRights = User.IsInRole("Admin") || User.IsInRole("GeneralManager") || User.IsInRole("Coach");

            if (!hasManagementRights && !isOwnProfile)
            {
                // Dacă ești un alt jucător sau un user simplu, nu ai voie să vezi detalii private (contracte etc)
                // Putem returna Forbid() sau doar view-ul limitat. Aici returnăm View-ul, dar ascundem butoanele în HTML.
                // Dacă vrei strictețe maximă: return Forbid();
            }

            return View(player);
        }

        // --- 3. ADAUGĂ JUCĂTOR (CREATE) ---
        [HttpGet]
        public IActionResult Create()
        {
            // Verificare Permisiune
            if (!User.HasClaim("Permission", "Permissions.Players.Create") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Pregătim dropdown-urile pentru View
            // Notă: Presupunem că alegi un Staff existent sau creezi unul nou. 
            // Aici e un exemplu simplu unde selectezi Echipa.
            ViewData["TeamId"] = new SelectList(_context.Teams, "TeamId", "Name");

            // Dacă ai nevoie să selectezi un membru Staff care nu e încă jucător:
            // ViewData["StaffId"] = new SelectList(_context.Staff, "StaffId", "LastName");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Player player)
        {
            if (!User.HasClaim("Permission", "Permissions.Players.Create") && !User.IsInRole("Admin"))
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

            ViewData["TeamId"] = new SelectList(_context.Teams, "TeamId", "Name", player.CurrentTeamId);
            return View(player);
        }

        // --- 4. EDITEAZĂ JUCĂTOR (UPDATE) ---
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            // Verificare Permisiune
            if (!User.HasClaim("Permission", "Permissions.Players.Edit") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var player = await _context.Players.Include(p => p.Staff).FirstOrDefaultAsync(p => p.PlayerId == id);
            if (player == null) return NotFound();

            // Trimitem lista de echipe pentru Dropdown
            ViewData["TeamId"] = new SelectList(_context.Teams, "TeamId", "Name", player.CurrentTeamId);

            return View(player);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Player player)
        {
            if (id != player.PlayerId) return NotFound();

            // Verificare Permisiune
            if (!User.HasClaim("Permission", "Permissions.Players.Edit") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(player);
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

            ViewData["TeamId"] = new SelectList(_context.Teams, "TeamId", "Name", player.CurrentTeamId);
            return View(player);
        }

        // --- 5. ȘTERGE JUCĂTOR (DELETE) ---
        [HttpGet] // Opțional, pentru pagina de confirmare
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            if (!User.HasClaim("Permission", "Permissions.Players.Delete") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var player = await _context.Players
                .Include(p => p.Staff)
                .Include(p => p.CurrentTeam)
                .FirstOrDefaultAsync(m => m.PlayerId == id);

            if (player == null) return NotFound();

            return View(player);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!User.HasClaim("Permission", "Permissions.Players.Delete") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

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

        // --- 6. PROFILUL MEU (HELPER PENTRU JUCĂTORI) ---
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);

            var staffMember = await _context.Staff
                .Include(s => s.Player)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (staffMember == null || staffMember.Player == null)
            {
                // Dacă nu ești jucător, te trimitem Acasă
                return RedirectToAction("Index", "Home");
            }

            // Te trimitem la metoda Details cu ID-ul tău de jucător
            return RedirectToAction("Details", new { id = staffMember.Player.PlayerId });
        }

        // --- METODE AJUTĂTOARE ---

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
                // Notă: SaveChanges se face de obicei în metoda principală, dar e ok și aici dacă nu sunt tranzacții complexe
            }
        }
    }
}
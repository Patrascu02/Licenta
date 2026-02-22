using Licenta.Data;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // --- 1. LISTA ANTRENORI (Admin / GeneralManager) ---
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Verificăm dacă userul are dreptul să vadă lista (Admin sau permisiune specifică)
            if (!User.IsInRole("Admin") && !User.HasClaim("Permission", "Coaches.View"))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var coaches = await _context.Coaches
                .Include(c => c.Staff) // Încărcăm datele personale (Nume, Prenume)
                .ToListAsync();

            return View(coaches);
        }

        // --- 2. PROFILUL MEU (Pentru Antrenorul Logat) ---
        [HttpGet]
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);

            // Căutăm antrenorul asociat userului curent
            var staffMember = await _context.Staff
                .Include(s => s.Coach)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (staffMember == null || staffMember.Coach == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Redirecționăm către pagina de detalii cu ID-ul corect
            return RedirectToAction("Details", new { id = staffMember.Coach.CoachId });
        }

        // --- 3. DETALII ANTRENOR (Vizualizare Profil) ---
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var coach = await _context.Coaches
                .Include(c => c.Staff)
                .FirstOrDefaultAsync(c => c.CoachId == id);

            if (coach == null) return NotFound();

            // SECURITATE: Cine are voie să vadă acest profil?
            // 1. Adminul
            // 2. Antrenorul însuși (Profilul propriu)

            var currentUserId = _userManager.GetUserId(User);
            bool isOwnProfile = (coach.Staff.UserId == currentUserId);
            bool isAdmin = User.IsInRole("Admin") || User.HasClaim("Permission", "Coaches.View");

            if (!isAdmin && !isOwnProfile)
            {
                return Forbid();
            }

            return View(coach);
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
                .Where(g => g.GameDate >= DateTime.Now)
                .OrderBy(g => g.GameDate)
                .Take(4)
                .ToListAsync();

            var recentGames = await _context.Games
                .Where(g => g.GameDate < DateTime.Now)
                .OrderByDescending(g => g.GameDate)
                .Take(5)
                .ToListAsync();

            var model = new CoachDashboardViewModel
            {
                StaffInfo = staff,
                CoachInfo = staff.Coach,
                UpcomingGames = upcomingGames,
                RecentGames = recentGames,
                TotalPlayers = totalPlayers,
                InjuredPlayersCount = injuredCount
            };

            return View(model);
        }


    }
}
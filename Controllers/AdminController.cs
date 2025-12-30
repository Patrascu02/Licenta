using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Licenta.Data;
using Microsoft.AspNetCore.Identity;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> AdminDashboard()
        {
            // Statistici pentru carduri
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalPlayers = await _context.Players.CountAsync();
            ViewBag.TotalCoaches = await _context.Coaches.CountAsync();
            ViewBag.TotalStaff = await _context.Staff.CountAsync();

            // Luam ultimele log-uri de audit pentru a arata activitatea recenta
            var recentLogs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(5)
                .ToListAsync();

            return View(recentLogs);
        }

        
        public IActionResult CreateUser()
        {
            return View();
        }
    }
}
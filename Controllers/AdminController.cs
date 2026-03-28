using Licenta.Data;
using Licenta.Models.Communication;
using Licenta.Models.Core;
using Licenta.Models.Roles;
using Licenta.Models.Security;
using Licenta.Models.Sports;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Licenta.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _env;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _env = env;
        }

        // --- DASHBOARD ---
        public async Task<IActionResult> AdminDashboard()
        {
            // 1. Statistici de Baza
            var totalUsers = await _context.Users.CountAsync();
            var totalPlayers = await _context.Players.CountAsync();
            var totalCoaches = await _context.Coaches.CountAsync();
            var totalStaff = await _context.Staff.CountAsync();

            // 2. RAM Usage
            var currentProcess = Process.GetCurrentProcess();
            double ramUsedMb = Math.Round(currentProcess.WorkingSet64 / 1024.0 / 1024.0, 2);
            double ramLimitMb = 1024;

            // 3. Storage Usage
            double storageUsedMb = 0;
            try
            {
                var webRootPath = _env.WebRootPath;
                if (Directory.Exists(webRootPath))
                {
                    long bytes = Directory.GetFiles(webRootPath, "*", SearchOption.AllDirectories)
                                          .Sum(t => (new FileInfo(t).Length));
                    storageUsedMb = Math.Round(bytes / 1024.0 / 1024.0, 2);
                }
            }
            catch { storageUsedMb = 0; }
            double storageLimitMb = 500;

            // 4. Sesiuni Active
            var activeThreshold = DateTime.Now.AddMinutes(-30);
            var activeUsersCount = await _context.AuditLogs
                .Where(l => l.Timestamp >= activeThreshold)
                .Select(l => l.StaffId)
                .Distinct()
                .CountAsync();

            if (activeUsersCount == 0) activeUsersCount = 1;

            // 5. Audit Logs Recente
            var recentLogs = await _context.AuditLogs
                .Include(a => a.Staff)
                .OrderByDescending(l => l.Timestamp)
                .Take(6)
                .ToListAsync();

            // Ultima Activitate
            var lastLog = recentLogs.FirstOrDefault();
            string lastActivityStr = lastLog != null ? lastLog.Timestamp.ToString("HH:mm") : "--:--";

            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalPlayers = totalPlayers,
                TotalCoaches = totalCoaches,
                TotalStaff = totalStaff,
                RamUsageMb = ramUsedMb,
                RamTotalMb = ramLimitMb,
                StorageUsageMb = storageUsedMb,
                StorageLimitMb = storageLimitMb,
                ActiveSessions = activeUsersCount,
                LastActivityTime = lastActivityStr,
                CpuUsagePercent = new Random().Next(5, 20),
                RecentLogs = recentLogs
            };

            return View(model);
        }

        // --- CREARE UTILIZATOR (GET) ---
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // --- CREARE UTILIZATOR (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Această adresă de email este deja utilizată.");
                    return View(model);
                }

                var user = new IdentityUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);

                    var staff = new Staff
                    {
                        UserId = user.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        DateOfBirth = model.DateOfBirth,
                        HireDate = model.HireDate,
                        ExperienceYears = 0
                    };
                    _context.Staff.Add(staff);
                    await _context.SaveChangesAsync();

                    if (model.Role == "Player")
                    {
                        var mainTeam = await _context.Teams.FirstOrDefaultAsync();
                        var player = new Player
                        {
                            StaffId = staff.StaffId,
                            Position = model.Position,
                            JerseyNumber = model.JerseyNumber ?? 0,
                            Height = model.Height ?? 0,
                            Weight = model.Weight ?? 0,
                            CurrentTeamId = mainTeam?.TeamId
                        };
                        _context.Players.Add(player);
                        await _context.SaveChangesAsync();

                        var history = new PlayerTeamHistory
                        {
                            PlayerId = player.PlayerId,
                            TeamId = mainTeam.TeamId,
                            StartDate = DateTime.Now
                        };
                        _context.PlayerTeamHistories.Add(history);
                    }
                    else if (model.Role == "Coach")
                    {
                        _context.Coaches.Add(new Coach
                        {
                            StaffId = staff.StaffId,
                            LicenseNumber = model.LicenseNumber ?? "Fără Licență"
                        });
                    }
                    else if (model.Role == "Medic")
                    {
                        _context.Medics.Add(new Medic
                        {
                            StaffId = staff.StaffId,
                            Specialty = model.Specialization,
                            MedicalLicense = "În așteptare"
                        });
                    }
                    else if (model.Role == "Scout")
                    {
                        _context.Scouts.Add(new Scout { StaffId = staff.StaffId });
                    }
                    else if (model.Role == "GeneralManager")
                    {
                        _context.GeneralManagers.Add(new GeneralManager
                        {
                            StaffId = staff.StaffId,
                            Office = model.Office ?? "Administrație"
                        });
                    }

                    await LogAuditAction($"Creat utilizator nou: {model.Role} - {model.FirstName} {model.LastName}");
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(AdminDashboard));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // --- VERIFICARE EMAIL (AJAX) ---
        [HttpGet]
        public async Task<JsonResult> CheckEmailExists(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return Json(new { exists = (user != null) });
        }

        // --- GESTIUNE UTILIZATORI ---
        public async Task<IActionResult> ManageUsers()
        {
            var staffList = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .Include(s => s.Scout)
                .Include(s => s.GeneralManager)
                .ToListAsync();

            var viewModel = new List<UserManagementViewModel>();

            foreach (var staff in staffList)
            {
                var identityUser = await _userManager.FindByIdAsync(staff.UserId);
                if (identityUser == null) continue;

                var roles = await _userManager.GetRolesAsync(identityUser);
                var roleName = roles.FirstOrDefault() ?? "Nespecificat";

                if (roleName == "Admin") continue;

                viewModel.Add(new UserManagementViewModel
                {
                    StaffMember = staff,
                    RoleName = roleName,
                    Email = identityUser.Email
                });
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var staff = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .Include(s => s.GeneralManager) 
                .FirstOrDefaultAsync(s => s.StaffId == id);

            if (staff == null) return NotFound();

            var identityUser = await _userManager.FindByIdAsync(staff.UserId);
            var roles = await _userManager.GetRolesAsync(identityUser);
            var roleName = roles.FirstOrDefault() ?? "Nespecificat";

            var model = new EditUserViewModel
            {
                StaffId = staff.StaffId,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = identityUser?.Email,
                DateOfBirth = staff.DateOfBirth,
                HireDate = staff.HireDate,
                RoleName = roleName,

                Position = staff.Player?.Position,
                JerseyNumber = staff.Player?.JerseyNumber,
                Height = staff.Player?.Height,
                Weight = staff.Player?.Weight,

                LicenseNumber = staff.Coach?.LicenseNumber,
                Specialization = staff.Medic?.Specialty,
                Office = staff.GeneralManager?.Office 
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var staff = await _context.Staff
                .Include(s => s.Player)
                .Include(s => s.Coach)
                .Include(s => s.Medic)
                .Include(s => s.GeneralManager) 
                .FirstOrDefaultAsync(s => s.StaffId == model.StaffId);

            if (staff == null) return NotFound();

            staff.FirstName = model.FirstName;
            staff.LastName = model.LastName;
            staff.DateOfBirth = model.DateOfBirth;
            staff.HireDate = model.HireDate;

            if (model.RoleName == "Player" && staff.Player != null)
            {
                staff.Player.Position = model.Position;
                staff.Player.JerseyNumber = model.JerseyNumber ?? 0;
                staff.Player.Height = model.Height ?? 0;
                staff.Player.Weight = model.Weight ?? 0;
                _context.Update(staff.Player);
            }
            else if (model.RoleName == "Coach" && staff.Coach != null)
            {
                staff.Coach.LicenseNumber = model.LicenseNumber;
                _context.Update(staff.Coach);
            }
            else if (model.RoleName == "Medic" && staff.Medic != null)
            {
                staff.Medic.Specialty = model.Specialization;
                _context.Update(staff.Medic);
            }
            else if (model.RoleName == "GeneralManager" && staff.GeneralManager != null)
            {
                staff.GeneralManager.Office = model.Office;
                _context.Update(staff.GeneralManager);
            }

            _context.Update(staff);
            await LogAuditAction($"Editat utilizator: {staff.FirstName} {staff.LastName}");
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ManageUsers));
        }

        // --- DELETE USER ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var staffMember = await _context.Staff.FindAsync(id);
            if (staffMember == null) return NotFound();

            await LogAuditAction($"ȘTERS utilizator: {staffMember.FirstName} {staffMember.LastName} (ID: {id})");

            var user = await _userManager.FindByIdAsync(staffMember.UserId);
            _context.Staff.Remove(staffMember);

            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageUsers));
        }

        // --- HELPER LOG ---
        private async Task LogAuditAction(string message)
        {
            var currentUserId = _userManager.GetUserId(User);
            var adminStaff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == currentUserId);

            if (adminStaff != null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = message,
                    Timestamp = DateTime.Now,
                    StaffId = adminStaff.StaffId,
                    EntityName = "UserManagement"
                });
            }
        }

        // ==========================================
        //         MODUL PERMISIUNI (ACL)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> ManageRoles()
        {
            var roles = await _roleManager.Roles.Where(r => r.Name != "Admin").ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public async Task<IActionResult> ManagePermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();

            var allPermissions = await _context.Permissions.ToListAsync();
            var existingLinkIds = await _context.RolePermissions
                                          .Where(rp => rp.RoleId == roleId)
                                          .Select(rp => rp.PermissionId)
                                          .ToListAsync();

            var model = new ManageRolePermissionsViewModel
            {
                RoleId = roleId,
                RoleName = role.Name,
                PermissionList = allPermissions.Select(p => new PermissionCheckbox
                {
                    PermissionId = p.PermissionId,
                    Name = p.Name,
                    Description = p.Description,
                    IsSelected = existingLinkIds.Contains(p.PermissionId)
                }).ToList()
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePermissions(ManageRolePermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound();

            var oldLinks = await _context.RolePermissions.Where(rp => rp.RoleId == model.RoleId).ToListAsync();
            _context.RolePermissions.RemoveRange(oldLinks);

            var oldClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in oldClaims.Where(c => c.Type == "Permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }

            // FIX: Extragem denumirile corecte direct din Baza de Date pe baza ID-urilor bifate
            var selectedIds = model.PermissionList.Where(p => p.IsSelected).Select(p => p.PermissionId).ToList();
            var dbPermissions = await _context.Permissions.Where(p => selectedIds.Contains(p.PermissionId)).ToListAsync();

            foreach (var perm in dbPermissions)
            {
                _context.RolePermissions.Add(new RolePermission { RoleId = model.RoleId, PermissionId = perm.PermissionId });
                // Salvăm numele corect din baza de date, nu cel din interfață
                await _roleManager.AddClaimAsync(role, new Claim("Permission", perm.Name));
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageRoles));
        }

        // --- 2. GESTIUNE PERMISIUNI INDIVIDUALE (USER) ---
        [HttpGet]
        public async Task<IActionResult> ManageUserPermissions(int staffId)
        {
            var staff = await _context.Staff.FindAsync(staffId);
            if (staff == null) return NotFound();

            var userId = staff.UserId;
            var user = await _userManager.FindByIdAsync(userId);

            var allPermissions = await _context.Permissions.ToListAsync();

            var userDirectPermissions = await _context.UserPermissions
                .Where(up => up.UserId == userId)
                .Select(up => up.PermissionId)
                .ToListAsync();

            var userRoles = await _userManager.GetRolesAsync(user);
            var roleName = userRoles.FirstOrDefault() ?? "Nespecificat";

            var roleIds = await _roleManager.Roles
                .Where(r => userRoles.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var inheritedPermissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var model = new ManageUserPermissionsViewModel
            {
                StaffId = staff.StaffId,
                UserName = $"{staff.FirstName} {staff.LastName}",
                UserId = userId,
                RoleName = roleName, 
                PermissionList = allPermissions.Select(p => new PermissionCheckbox
                {
                    PermissionId = p.PermissionId,
                    Name = p.Name,
                    Description = p.Description,
                    IsSelected = userDirectPermissions.Contains(p.PermissionId),
                    IsInherited = inheritedPermissions.Contains(p.PermissionId)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserPermissions(ManageUserPermissionsViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var oldPermissions = await _context.UserPermissions.Where(up => up.UserId == model.UserId).ToListAsync();
            _context.UserPermissions.RemoveRange(oldPermissions);

            var oldClaims = await _userManager.GetClaimsAsync(user);
            foreach (var claim in oldClaims.Where(c => c.Type == "Permission"))
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            // FIX: Extragem denumirile corecte direct din Baza de Date pe baza ID-urilor bifate
            var selectedIds = model.PermissionList.Where(p => p.IsSelected).Select(p => p.PermissionId).ToList();
            var dbPermissions = await _context.Permissions.Where(p => selectedIds.Contains(p.PermissionId)).ToListAsync();

            foreach (var perm in dbPermissions)
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = model.UserId,
                    PermissionId = perm.PermissionId
                });

                // Salvăm numele corect din baza de date, nu cel din interfață
                await _userManager.AddClaimAsync(user, new Claim("Permission", perm.Name));
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManageUsers");
        }


        // ==========================================
        //         MODUL COMUNICATII (GRUPURI)
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> CreateMessageGroup()
        {
            var staffList = await _context.Staff.ToListAsync();
            var model = new CreateMessageGroupViewModel();

            foreach (var staff in staffList)
            {
                var user = await _userManager.FindByIdAsync(staff.UserId);
                if (user == null) continue;

                var roles = await _userManager.GetRolesAsync(user);

                model.AvailableStaff.Add(new StaffCheckboxItem
                {
                    StaffId = staff.StaffId,
                    FullName = $"{staff.FirstName} {staff.LastName}",
                    RoleName = roles.FirstOrDefault() ?? "Staff"
                });
            }

            model.AvailableStaff = model.AvailableStaff.OrderBy(s => s.RoleName).ThenBy(s => s.FullName).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMessageGroup(CreateMessageGroupViewModel model)
        {
            var selectedIds = model.AvailableStaff.Where(s => s.IsSelected).Select(s => s.StaffId).ToList();

            if (string.IsNullOrWhiteSpace(model.Name) || !selectedIds.Any())
            {
                ModelState.AddModelError("", "Numele este obligatoriu și trebuie selectat cel puțin un membru.");
                return View(model);
            }

            var group = new MessageGroup { Name = model.Name, CreatedAt = DateTime.Now };
            _context.MessageGroups.Add(group);
            await _context.SaveChangesAsync(); 

            foreach (var staffId in selectedIds)
            {
                _context.MessageGroupMembers.Add(new MessageGroupMember
                {
                    GroupId = group.GroupId,
                    StaffId = staffId
                });
            }

            await _context.SaveChangesAsync();
            await LogAuditAction($"A creat grupul de mesaje '{group.Name}' cu {selectedIds.Count} membri.");

            return RedirectToAction("AdminDashboard");
        }

        // --- AFISEAZA TOATE GRUPURILE ---
        [HttpGet]
        public async Task<IActionResult> ManageMessageGroups()
        {
            var groups = await _context.MessageGroups
                .Include(g => g.Members)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return View(groups);
        }

        // --- EDITARE GRUP (GET) ---
        [HttpGet]
        public async Task<IActionResult> EditMessageGroup(int id)
        {
            var group = await _context.MessageGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null) return NotFound();

            var staffList = await _context.Staff.ToListAsync();
            var model = new EditMessageGroupViewModel
            {
                GroupId = group.GroupId,
                Name = group.Name
            };

            foreach (var staff in staffList)
            {
                var user = await _userManager.FindByIdAsync(staff.UserId);
                if (user == null) continue;
                var roles = await _userManager.GetRolesAsync(user);

                model.AvailableStaff.Add(new StaffCheckboxItem
                {
                    StaffId = staff.StaffId,
                    FullName = $"{staff.FirstName} {staff.LastName}",
                    RoleName = roles.FirstOrDefault() ?? "Staff",
                    IsSelected = group.Members.Any(m => m.StaffId == staff.StaffId)
                });
            }

            model.AvailableStaff = model.AvailableStaff.OrderBy(s => s.RoleName).ThenBy(s => s.FullName).ToList();
            return View(model);
        }

        // --- EDITARE GRUP (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMessageGroup(EditMessageGroupViewModel model)
        {
            var group = await _context.MessageGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupId == model.GroupId);

            if (group == null) return NotFound();

            var selectedIds = model.AvailableStaff.Where(s => s.IsSelected).Select(s => s.StaffId).ToList();

            if (string.IsNullOrWhiteSpace(model.Name) || !selectedIds.Any())
            {
                ModelState.AddModelError("", "Numele este obligatoriu și trebuie selectat cel puțin un membru.");
                return View(model);
            }

            group.Name = model.Name;

            var membersToRemove = group.Members.Where(m => !selectedIds.Contains(m.StaffId)).ToList();
            _context.MessageGroupMembers.RemoveRange(membersToRemove);

            var existingIds = group.Members.Select(m => m.StaffId).ToList();
            foreach (var id in selectedIds)
            {
                if (!existingIds.Contains(id))
                {
                    _context.MessageGroupMembers.Add(new MessageGroupMember
                    {
                        GroupId = group.GroupId,
                        StaffId = id,
                        LastReadAt = DateTime.Now 
                    });
                }
            }

            await _context.SaveChangesAsync();
            await LogAuditAction($"A editat grupul de mesaje '{group.Name}' (ID: {group.GroupId}).");

            return RedirectToAction(nameof(ManageMessageGroups));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessageGroup(int id)
        {
            var group = await _context.MessageGroups.FindAsync(id);
            if (group != null)
            {
                _context.MessageGroups.Remove(group);
                await LogAuditAction($"A șters grupul de mesaje '{group.Name}'.");
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageMessageGroups));
        }

        [HttpGet]
        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _context.AuditLogs
                .Include(a => a.Staff)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            return View(logs);
        }

        [HttpGet]
        public async Task<IActionResult> ManageGames()
        {
            var games = await _context.Games
                .Include(g => g.Season)
                .OrderByDescending(g => g.GameDate)
                .ToListAsync();

            return View(games);
        }

        [HttpGet]
        public async Task<IActionResult> CreateGame()
        {
            ViewBag.Seasons = new SelectList(await _context.Seasons.ToListAsync(), "SeasonId", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGame(Game model)
        {
            if (ModelState.IsValid)
            {
                model.HomeScore = 0;
                model.AwayScore = 0;

                _context.Games.Add(model);
                await _context.SaveChangesAsync();
                await LogAuditAction($"A programat un meci nou pentru data {model.GameDate:dd MMM yyyy}.");

                return RedirectToAction(nameof(ManageGames));
            }

            ViewBag.Seasons = new SelectList(await _context.Seasons.ToListAsync(), "SeasonId", "Name", model.SeasonId);
            return View(model);
        }
    }
}
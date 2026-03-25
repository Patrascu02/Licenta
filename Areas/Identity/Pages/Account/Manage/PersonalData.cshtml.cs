using System;
using System.Linq;
using System.Threading.Tasks;
using Licenta.Data;
using Licenta.Models.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Licenta.Areas.Identity.Pages.Account.Manage
{
    public class PersonalDataModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<PersonalDataModel> _logger;
        private readonly ApplicationDbContext _context; 

        public PersonalDataModel(
            UserManager<IdentityUser> userManager,
            ILogger<PersonalDataModel> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        public Contract ActiveContract { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var staff = await _context.Staff
                .Include(s => s.Contracts)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (staff != null && staff.Contracts != null)
            {
                ActiveContract = staff.Contracts.FirstOrDefault(c => c.IsActive);
            }

            return Page();
        }
    }
}
using Licenta.Data;
using Licenta.Models.Communication;
using Licenta.Models.Core;
using Licenta.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Licenta.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public MessagesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Staff> GetCurrentStaffAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.Staff.FirstOrDefaultAsync(s => s.UserId == user.Id);
        }

        // --- INBOX (1-LA-1 ȘI GRUPURI) ---
        public async Task<IActionResult> Index()
        {
            var currentStaff = await GetCurrentStaffAsync();
            if (currentStaff == null) return RedirectToAction("Index", "Home");

            var conversations = new List<ConversationItem>();

            // 1. Aducem conversațiile 1-la-1
            var allOtherStaff = await _context.Staff.Where(s => s.StaffId != currentStaff.StaffId).ToListAsync();
            foreach (var staff in allOtherStaff)
            {
                var lastMessage = await _context.Messages
                    .Where(m => m.GroupId == null && ((m.FromStaffId == currentStaff.StaffId && m.ToStaffId == staff.StaffId) ||
                                                      (m.FromStaffId == staff.StaffId && m.ToStaffId == currentStaff.StaffId)))
                    .OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();

                var unreadCount = await _context.Messages
                    .CountAsync(m => m.GroupId == null && m.FromStaffId == staff.StaffId && m.ToStaffId == currentStaff.StaffId && !m.IsRead);

                conversations.Add(new ConversationItem { IsGroup = false, OtherStaff = staff, LastMessage = lastMessage, UnreadCount = unreadCount });
            }

            // 2. Aducem Grupurile
            var myMemberships = await _context.MessageGroupMembers
                .Include(mgm => mgm.Group)
                .Where(mgm => mgm.StaffId == currentStaff.StaffId)
                .ToListAsync();

            foreach (var membership in myMemberships)
            {
                var group = membership.Group;
                var lastGroupMsg = await _context.Messages
                    .Include(m => m.FromStaff)
                    .Where(m => m.GroupId == group.GroupId)
                    .OrderByDescending(m => m.SentAt).FirstOrDefaultAsync();

                // NOU: Calculăm mesajele necitite de acest membru în grup (trimise DUPĂ ultima lui vizită, și nu trimise de el)
                int unreadGroupCount = await _context.Messages
                    .CountAsync(m => m.GroupId == group.GroupId &&
                                     m.SentAt > membership.LastReadAt &&
                                     m.FromStaffId != currentStaff.StaffId);

                conversations.Add(new ConversationItem { IsGroup = true, Group = group, LastMessage = lastGroupMsg, UnreadCount = unreadGroupCount });
            }

            // 3. Sortăm totul
            var sortedConversations = conversations
                .OrderByDescending(c => c.UnreadCount > 0)
                .ThenByDescending(c => c.LastMessage?.SentAt ?? DateTime.MinValue)
                .ToList();

            return View(new InboxViewModel { CurrentStaff = currentStaff, Conversations = sortedConversations });
        }

        // --- CHAT 1-LA-1 ---
        public async Task<IActionResult> Chat(int id)
        {
            var currentStaff = await GetCurrentStaffAsync();
            var otherStaff = await _context.Staff.FindAsync(id);
            var unreadMessages = await _context.Messages.Where(m => m.FromStaffId == id && m.ToStaffId == currentStaff.StaffId && m.GroupId == null && !m.IsRead).ToListAsync();
            foreach (var msg in unreadMessages) msg.IsRead = true;
            await _context.SaveChangesAsync();

            var messages = await _context.Messages.Where(m => m.GroupId == null && ((m.FromStaffId == currentStaff.StaffId && m.ToStaffId == id) || (m.FromStaffId == id && m.ToStaffId == currentStaff.StaffId))).OrderBy(m => m.SentAt).ToListAsync();
            return View(new ChatViewModel { CurrentStaff = currentStaff, OtherStaff = otherStaff, Messages = messages });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int toStaffId, string text)
        {
            var currentStaff = await GetCurrentStaffAsync();
            _context.Messages.Add(new Message { FromStaffId = currentStaff.StaffId, ToStaffId = toStaffId, Text = text, SentAt = DateTime.Now, IsRead = false });
            await _context.SaveChangesAsync();
            return RedirectToAction("Chat", new { id = toStaffId });
        }

        // --- FEREASTRA DE CHAT DE GRUP ---
        public async Task<IActionResult> GroupChat(int id)
        {
            var currentStaff = await GetCurrentStaffAsync();
            var group = await _context.MessageGroups.Include(g => g.Members).ThenInclude(m => m.Staff).FirstOrDefaultAsync(g => g.GroupId == id);

            if (group == null) return RedirectToAction("Index");

            var membership = group.Members.FirstOrDefault(m => m.StaffId == currentStaff.StaffId);
            if (membership == null) return RedirectToAction("Index"); // Nu are acces

            // NOU: Actualizăm "LastReadAt" ca fiind ACUM, deci toate mesajele devin "citite"
            membership.LastReadAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var messages = await _context.Messages
                .Include(m => m.FromStaff)
                .Where(m => m.GroupId == id)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return View(new GroupChatViewModel { CurrentStaff = currentStaff, Group = group, Messages = messages });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendGroupMessage(int groupId, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return RedirectToAction("GroupChat", new { id = groupId });

            var currentStaff = await GetCurrentStaffAsync();
            _context.Messages.Add(new Message
            {
                FromStaffId = currentStaff.StaffId,
                GroupId = groupId,
                Text = text,
                SentAt = DateTime.Now,
                IsRead = true // Dummy
            });

            // Actualizăm timpul autorului ca să nu își vadă propriul mesaj ca "necitit"
            var membership = await _context.MessageGroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.StaffId == currentStaff.StaffId);
            if (membership != null) membership.LastReadAt = DateTime.Now.AddSeconds(2); // +2 sec ca să fim siguri că depășește SentAt

            await _context.SaveChangesAsync();
            return RedirectToAction("GroupChat", new { id = groupId });
        }
    }
}
using Licenta.Models.Communication;
using Licenta.Models.Core;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.ViewModels
{
    // --- MODELE PENTRU INBOX ---
    public class InboxViewModel
    {
        public Staff CurrentStaff { get; set; }
        public List<ConversationItem> Conversations { get; set; }
    }

    public class ConversationItem
    {
        public bool IsGroup { get; set; } // NOU: Să știm dacă e grup sau 1-la-1
        public Staff OtherStaff { get; set; }
        public MessageGroup Group { get; set; } // NOU
        public Message LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatViewModel
    {
        public Staff CurrentStaff { get; set; }
        public Staff OtherStaff { get; set; }
        public List<Message> Messages { get; set; }
    }

    // NOU: Model pentru fereastra de Chat de Grup
    public class GroupChatViewModel
    {
        public Staff CurrentStaff { get; set; }
        public MessageGroup Group { get; set; }
        public List<Message> Messages { get; set; }
    }

    // --- MODELE PENTRU ADMIN (CREARE GRUP) ---
    public class CreateMessageGroupViewModel
    {
        [Required(ErrorMessage = "Numele grupului este obligatoriu.")]
        [MaxLength(100)]
        public string Name { get; set; }

        public List<int> SelectedStaffIds { get; set; } = new List<int>();
        public List<StaffCheckboxItem> AvailableStaff { get; set; } = new List<StaffCheckboxItem>();
    }

    public class StaffCheckboxItem
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string RoleName { get; set; }
        public bool IsSelected { get; set; }
    }

    public class EditMessageGroupViewModel
    {
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Numele grupului este obligatoriu.")]
        [MaxLength(100)]
        public string Name { get; set; }

        public List<int> SelectedStaffIds { get; set; } = new List<int>();
        public List<StaffCheckboxItem> AvailableStaff { get; set; } = new List<StaffCheckboxItem>();
    }
}
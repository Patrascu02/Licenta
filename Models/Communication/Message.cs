using Licenta.Models.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace Licenta.Models.Communication
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        public string Text { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        public int FromStaffId { get; set; }
        public Staff FromStaff { get; set; }

        public int? ToStaffId { get; set; }
        public Staff ToStaff { get; set; }

        public int? GroupId { get; set; }
        public MessageGroup Group { get; set; }
    }
}
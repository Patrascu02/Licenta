using Licenta.Models.Core;

namespace Licenta.Models.Communication
{
    public class Message
    {
        public int MessageId { get; set; }
        public int FromStaffId { get; set; }
        public int ToStaffId { get; set; }
        public string? Text { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        public Staff? FromStaff { get; set; }
        public Staff? ToStaff { get; set; }
    }
}

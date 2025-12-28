using Licenta.Models.Core;

namespace Licenta.Models.Communication
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int StaffId { get; set; }
        public string? Type { get; set; }
        public string? Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public Staff? Staff { get; set; }
    }
}

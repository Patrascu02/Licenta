using Licenta.Models.Core;

namespace Licenta.Models.Security
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public int StaffId { get; set; }
        public string? Action { get; set; }
        public string? EntityName { get; set; }
        public int EntityId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime Timestamp { get; set; }

        public Staff? Staff { get; set; }
    }
}
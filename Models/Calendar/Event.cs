using Licenta.Models.Core;

namespace Licenta.Models.Calendar
{
    public class Event
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StaffId { get; set; }
        public string RelatedEntity { get; set; }

        public Staff Staff { get; set; }
    }
}

using Licenta.Models.Core;

namespace Licenta.Models.HR
{
    public class TerminationNotice
    {
        public int TerminationNoticeId { get; set; }
        public int IssuedByStaffId { get; set; }
        public int TargetStaffId { get; set; }
        public DateTime NoticeDate { get; set; }
        public DateTime EffectiveTerminationDate { get; set; }
        public string Reason { get; set; }
        public bool IsFinalized { get; set; }

        public Staff IssuedByStaff { get; set; }
        public Staff TargetStaff { get; set; }
    }
}

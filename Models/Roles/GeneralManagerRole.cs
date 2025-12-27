using Licenta.Models.Communication;
using Licenta.Models.Core;
using Licenta.Models.Finance;
using Licenta.Models.HR;

namespace Licenta.Models.Roles
{
    public class GeneralManager
    {
        public int GeneralManagerId { get; set; }
        public int StaffId { get; set; }
        public string Office { get; set; }

        public Staff Staff { get; set; }

        // GM specific capabilities
        public ICollection<Expense> ManagedBudgets { get; set; }
        public ICollection<TerminationNotice> Approvals { get; set; }
    }
}

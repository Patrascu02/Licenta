using Licenta.Models.Core;

namespace Licenta.Models.Roles
{
    public class Medic
    {
        public int MedicId { get; set; }
        public int StaffId { get; set; }
        public string? Specialty { get; set; }
        public string? MedicalLicense { get; set; }

        public Staff? Staff { get; set; }
    }
}

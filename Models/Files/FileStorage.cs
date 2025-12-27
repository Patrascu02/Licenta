using Licenta.Models.Core;

namespace Licenta.Models.Files
{
    public class FileStorage
    {
        public int FileId { get; set; }
        public int StaffId { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public string RelatedEntity { get; set; }
        public DateTime UploadedAt { get; set; }

        public Staff Staff { get; set; }
    }
}

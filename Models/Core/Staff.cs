using Licenta.Models.Communication;
using Licenta.Models.Files;
using Licenta.Models.HR;
using Licenta.Models.Identity;
using Licenta.Models.Roles;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Licenta.Models.Core
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime HireDate { get; set; }
        public int ExperienceYears { get; set; }

        public ApplicationUser? User { get; set; }
        public ICollection<StaffRole>? StaffRoles { get; set; }
        public GeneralManager? GeneralManager { get; set; }
        public Player? Player { get; set; }
        public Coach? Coach { get; set; }
        public Medic? Medic { get; set; }
        public Scout? Scout { get; set; }
        public ICollection<Message>? MessagesSent { get; set; }
        public ICollection<Message>? MessagesReceived { get; set; }
        public ICollection<Notification>? Notifications { get; set; }
        public ICollection<FileStorage>? Files { get; set; }
        public ICollection<TerminationNotice>? NoticesIssued { get; set; }
        public ICollection<TerminationNotice>? NoticesReceived { get; set; }
    }
}

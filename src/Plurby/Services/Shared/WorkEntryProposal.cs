using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Plurby.Services.Shared
{
    public enum ProposalStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class WorkEntryProposal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid WorkEntryId { get; set; }
        public WorkEntry WorkEntry { get; set; }

        public Guid ProposedByUserId { get; set; }
        public User ProposedByUser { get; set; }

        public DateTime ProposedStartTime { get; set; }
        public DateTime? ProposedEndTime { get; set; }

        public ProposalStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Guid? ProcessedByUserId { get; set; }
        public User ProcessedByUser { get; set; }
    }
}

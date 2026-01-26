using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plurby.Services.Shared
{
    public class WorkHistoryQuery
    {
        public Guid UserId { get; set; }
    }

    public class WorkHistoryDTO
    {
        public Guid Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool HasPendingProposal { get; set; }
        public Guid? ProposalId { get; set; }
        public DateTime? ProposedStartTime { get; set; }
        public DateTime? ProposedEndTime { get; set; }
        public bool IsNewEntryProposal { get; set; }
    }

    public class WorkEntryProposalsQuery
    {
        public Guid UserId { get; set; }
    }

    public class PendingProposalsForManagerQuery
    {
        // No parameters needed - gets all pending proposals for manager to review
    }

    public class PendingProposalDTO
    {
        public Guid Id { get; set; }
        public Guid? WorkEntryId { get; set; }
        public Guid ProposedByUserId { get; set; }
        public string ProposedByUserName { get; set; }
        public DateTime ProposedStartTime { get; set; }
        public DateTime? ProposedEndTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsNewEntryProposal { get; set; }
    }

    public class WorkEntryProposalDTO
    {
        public Guid Id { get; set; }
        public Guid? WorkEntryId { get; set; }
        public Guid ProposedByUserId { get; set; }
        public DateTime ProposedStartTime { get; set; }
        public DateTime? ProposedEndTime { get; set; }
        public ProposalStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public Guid? ProcessedByUserId { get; set; }
    }

    public class CurrentWorkStatusQuery
    {
        public Guid UserId { get; set; }
    }

    public class CurrentWorkStatusDTO
    {
        public bool IsWorking { get; set; }
        public DateTime? StartTime { get; set; }
    }

    public partial class SharedService
    {
        public async Task<IEnumerable<WorkHistoryDTO>> Query(WorkHistoryQuery qry)
        {
            var entries = await _dbContext.WorkEntries
                .Where(x => x.UserId == qry.UserId)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();

            var entryIds = entries.Select(x => x.Id).ToList();
            var existingEntryProposals = await _dbContext.WorkEntryProposals
                .Where(x => x.WorkEntryId.HasValue && entryIds.Contains(x.WorkEntryId.Value) && x.Status == ProposalStatus.Pending)
                .ToListAsync();

            var newEntryProposals = await _dbContext.WorkEntryProposals
                .Where(x => !x.WorkEntryId.HasValue && x.ProposedByUserId == qry.UserId && x.Status == ProposalStatus.Pending)
                .ToListAsync();

            var existingProposalDict = existingEntryProposals.ToDictionary(x => x.WorkEntryId.Value);

            // Start with existing entries
            var result = entries.Select(x =>
            {
                var proposal = existingProposalDict.GetValueOrDefault(x.Id);
                return new WorkHistoryDTO
                {
                    Id = x.Id,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    Duration = x.EndTime.HasValue ? (x.EndTime.Value - x.StartTime) : null,
                    HasPendingProposal = proposal != null && proposal.Status == ProposalStatus.Pending,
                    ProposalId = proposal?.Id,
                    ProposedStartTime = proposal?.Status == ProposalStatus.Pending ? proposal.ProposedStartTime : null,
                    ProposedEndTime = proposal?.Status == ProposalStatus.Pending ? proposal.ProposedEndTime : null
                };
            }).ToList();

            // Add new entry proposals as virtual entries
            var newEntryDTOs = newEntryProposals.Select(x => new WorkHistoryDTO
            {
                Id = Guid.Empty, // Use empty GUID to indicate it's a new entry proposal
                StartTime = x.ProposedStartTime,
                EndTime = x.ProposedEndTime,
                Duration = x.ProposedEndTime.HasValue ? (x.ProposedEndTime.Value - x.ProposedStartTime) : null,
                HasPendingProposal = false, // This IS the proposal, not a work entry with a proposal
                ProposalId = x.Id,
                ProposedStartTime = x.ProposedStartTime,
                ProposedEndTime = x.ProposedEndTime,
                IsNewEntryProposal = true // Add flag to identify new entry proposals
            }).ToList();

            // Combine and sort by start time
            result.AddRange(newEntryDTOs);
            return result.OrderByDescending(x => x.StartTime).ToArray();
        }

        public async Task<CurrentWorkStatusDTO> Query(CurrentWorkStatusQuery qry)
        {
            var entry = await _dbContext.WorkEntries
                .Where(x => x.UserId == qry.UserId && x.EndTime == null)
                .OrderByDescending(x => x.StartTime)
                .FirstOrDefaultAsync();

            return new CurrentWorkStatusDTO
            {
                IsWorking = entry != null,
                StartTime = entry?.StartTime
            };
        }
    }
}
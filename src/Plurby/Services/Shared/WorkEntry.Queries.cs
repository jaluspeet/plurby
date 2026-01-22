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
            var proposals = await _dbContext.WorkEntryProposals
                .Where(x => entryIds.Contains(x.WorkEntryId) && x.Status == ProposalStatus.Pending)
                .ToListAsync();

            var proposalDict = proposals.ToDictionary(x => x.WorkEntryId);

            return entries.Select(x =>
            {
                var proposal = proposalDict.GetValueOrDefault(x.Id);
                return new WorkHistoryDTO
                {
                    Id = x.Id,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    Duration = x.EndTime.HasValue ? (x.EndTime.Value - x.StartTime) : null,
                    HasPendingProposal = proposal != null,
                    ProposalId = proposal?.Id,
                    ProposedStartTime = proposal?.ProposedStartTime,
                    ProposedEndTime = proposal?.ProposedEndTime
                };
            }).ToArray();
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
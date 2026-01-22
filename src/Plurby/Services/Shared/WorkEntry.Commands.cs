using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Plurby.Services.Shared
{
    public class StartWorkCommand
    {
        public Guid UserId { get; set; }
    }

    public class EndWorkCommand
    {
        public Guid UserId { get; set; }
    }

    public class UpdateWorkEntryCommand
    {
        public Guid WorkEntryId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class ProposeWorkEntryChangeCommand
    {
        public Guid WorkEntryId { get; set; }
        public Guid ProposedByUserId { get; set; }
        public DateTime ProposedStartTime { get; set; }
        public DateTime? ProposedEndTime { get; set; }
    }

    public class AcceptProposalCommand
    {
        public Guid ProposalId { get; set; }
        public Guid ProcessedByUserId { get; set; }
    }

    public class RejectProposalCommand
    {
        public Guid ProposalId { get; set; }
        public Guid ProcessedByUserId { get; set; }
    }

    public partial class SharedService
    {
        public async Task Handle(StartWorkCommand cmd)
        {
            var hasOpenEntry = await _dbContext.WorkEntries
                .AnyAsync(x => x.UserId == cmd.UserId && x.EndTime == null);

            if (hasOpenEntry) return; // Already working

            var entry = new WorkEntry
            {
                UserId = cmd.UserId,
                StartTime = DateTime.UtcNow
            };

            _dbContext.WorkEntries.Add(entry);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Handle(EndWorkCommand cmd)
        {
            var entry = await _dbContext.WorkEntries
                .Where(x => x.UserId == cmd.UserId && x.EndTime == null)
                .OrderByDescending(x => x.StartTime)
                .FirstOrDefaultAsync();

            if (entry == null) return; // Not working

            entry.EndTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }

        public async Task Handle(UpdateWorkEntryCommand cmd)
        {
            var entry = await _dbContext.WorkEntries
                .FirstOrDefaultAsync(x => x.Id == cmd.WorkEntryId);

            if (entry == null) return; // Entry not found

            entry.StartTime = cmd.StartTime.ToUniversalTime();
            entry.EndTime = cmd.EndTime?.ToUniversalTime();
            await _dbContext.SaveChangesAsync();
        }

        public async Task Handle(ProposeWorkEntryChangeCommand cmd)
        {
            var entry = await _dbContext.WorkEntries
                .FirstOrDefaultAsync(x => x.Id == cmd.WorkEntryId);

            if (entry == null) return; // Entry not found

            // Check if there's already a pending proposal for this entry
            var existingProposal = await _dbContext.WorkEntryProposals
                .FirstOrDefaultAsync(x => x.WorkEntryId == cmd.WorkEntryId && x.Status == ProposalStatus.Pending);

            if (existingProposal != null)
            {
                // Update existing proposal
                existingProposal.ProposedStartTime = cmd.ProposedStartTime.ToUniversalTime();
                existingProposal.ProposedEndTime = cmd.ProposedEndTime?.ToUniversalTime();
                existingProposal.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new proposal
                var proposal = new WorkEntryProposal
                {
                    WorkEntryId = cmd.WorkEntryId,
                    ProposedByUserId = cmd.ProposedByUserId,
                    ProposedStartTime = cmd.ProposedStartTime.ToUniversalTime(),
                    ProposedEndTime = cmd.ProposedEndTime?.ToUniversalTime(),
                    Status = ProposalStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.WorkEntryProposals.Add(proposal);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task Handle(AcceptProposalCommand cmd)
        {
            var proposal = await _dbContext.WorkEntryProposals
                .Include(x => x.WorkEntry)
                .FirstOrDefaultAsync(x => x.Id == cmd.ProposalId && x.Status == ProposalStatus.Pending);

            if (proposal == null) return; // Proposal not found or already processed

            // Update the work entry
            proposal.WorkEntry.StartTime = proposal.ProposedStartTime;
            proposal.WorkEntry.EndTime = proposal.ProposedEndTime;

            // Mark proposal as accepted
            proposal.Status = ProposalStatus.Accepted;
            proposal.ProcessedAt = DateTime.UtcNow;
            proposal.ProcessedByUserId = cmd.ProcessedByUserId;

            await _dbContext.SaveChangesAsync();
        }

        public async Task Handle(RejectProposalCommand cmd)
        {
            var proposal = await _dbContext.WorkEntryProposals
                .FirstOrDefaultAsync(x => x.Id == cmd.ProposalId && x.Status == ProposalStatus.Pending);

            if (proposal == null) return; // Proposal not found or already processed

            // Mark proposal as rejected
            proposal.Status = ProposalStatus.Rejected;
            proposal.ProcessedAt = DateTime.UtcNow;
            proposal.ProcessedByUserId = cmd.ProcessedByUserId;

            await _dbContext.SaveChangesAsync();
        }
    }
}
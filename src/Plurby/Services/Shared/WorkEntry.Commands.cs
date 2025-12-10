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
    }
}
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
        public double? DurationHours { get; set; }
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
            return await _dbContext.WorkEntries
                .Where(x => x.UserId == qry.UserId)
                .OrderByDescending(x => x.StartTime)
                .Select(x => new WorkHistoryDTO
                {
                    Id = x.Id,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    DurationHours = x.EndTime.HasValue ? (x.EndTime.Value - x.StartTime).TotalHours : null
                })
                .ToArrayAsync();
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
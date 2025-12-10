using Plurby.Services.Shared;
using System.Collections.Generic;

namespace Plurby.Web.Features.Employee
{
    public class EmployeeIndexViewModel
    {
        public CurrentWorkStatusDTO Status { get; set; }
        public IEnumerable<WorkHistoryDTO> History { get; set; }
    }
}
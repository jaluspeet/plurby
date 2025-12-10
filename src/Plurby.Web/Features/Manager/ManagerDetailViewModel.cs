using Plurby.Services.Shared;
using System.Collections.Generic;

namespace Plurby.Web.Features.Manager
{
    public class ManagerDetailViewModel
    {
        public UserDetailDTO User { get; set; }
        public IEnumerable<WorkHistoryDTO> History { get; set; }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plurby.Infrastructure;
using Plurby.Services.Shared;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Plurby.Web.Features.Manager
{
    [Authorize]
    public partial class ManagerController : Controller
    {
        private readonly SharedService _service;

        public ManagerController(SharedService service)
        {
            _service = service;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        public virtual async Task<IActionResult> Index(string filter, int page = 1)
        {
            var qry = new UsersIndexQuery
            {
                IdCurrentUser = CurrentUserId,
                Filter = filter,
                Paging = new Paging { Page = page, PageSize = 20 }
            };

            var result = await _service.Query(qry);
            return View(result);
        }

        public virtual async Task<IActionResult> Detail(Guid id)
        {
            var user = await _service.Query(new UserDetailQuery { Id = id });
            var history = await _service.Query(new WorkHistoryQuery { UserId = id });

            var model = new ManagerDetailViewModel
            {
                User = user,
                History = history
            };

            return View(model);
        }
    }
}
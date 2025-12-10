using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plurby.Services.Shared;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Plurby.Web.Features.Employee
{
    [Authorize]
    public partial class EmployeeController : Controller
    {
        private readonly SharedService _service;

        public EmployeeController(SharedService service)
        {
            _service = service;
        }

        private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        public virtual async Task<IActionResult> Index()
        {
            var status = await _service.Query(new CurrentWorkStatusQuery { UserId = CurrentUserId });
            var history = await _service.Query(new WorkHistoryQuery { UserId = CurrentUserId });

            var model = new EmployeeIndexViewModel
            {
                Status = status,
                History = history
            };

            return View(model);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Start()
        {
            await _service.Handle(new StartWorkCommand { UserId = CurrentUserId });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public virtual async Task<IActionResult> End()
        {
            await _service.Handle(new EndWorkCommand { UserId = CurrentUserId });
            return RedirectToAction(nameof(Index));
        }
    }
}
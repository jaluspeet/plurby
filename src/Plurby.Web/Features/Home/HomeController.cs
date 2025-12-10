using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plurby.Infrastructure;
using Plurby.Services.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plurby.Web.Features.Home
{
    [Authorize]
    public partial class HomeController : Controller
    {
        private readonly SharedService _service;

        public HomeController(SharedService service)
        {
            _service = service;
        }

        public virtual async Task<IActionResult> Index()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            var user = await _service.Query(new UserByEmailQuery { Email = email });
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            if (user.Role == UserRole.Manager)
            {
                var employees = await _service.Query(new UsersIndexQuery
                {
                    IdCurrentUser = user.Id,
                    Role = UserRole.Employee,
                    Paging = new Paging { PageSize = 100 }
                });
                return View("ManagerDashboard", employees);
            }
            else
            {
                var status = await _service.Query(new CurrentWorkStatusQuery { UserId = user.Id });
                var history = await _service.Query(new WorkHistoryQuery { UserId = user.Id });

                var model = new EmployeeDashboardViewModel
                {
                    Status = status,
                    History = history
                };
                return View("EmployeeDashboard", model);
            }
        }

        [HttpPost]
        public virtual async Task<IActionResult> StartWork()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            var user = await _service.Query(new UserByEmailQuery { Email = email });
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            await _service.Handle(new StartWorkCommand { UserId = user.Id });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public virtual async Task<IActionResult> EndWork()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            var user = await _service.Query(new UserByEmailQuery { Email = email });
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            await _service.Handle(new EndWorkCommand { UserId = user.Id });
            return RedirectToAction(nameof(Index));
        }

        public virtual async Task<IActionResult> EmployeeDetail(Guid id)
        {
            var user = await _service.Query(new UserDetailQuery { Id = id });
            var history = await _service.Query(new WorkHistoryQuery { UserId = id });

            return View(new EmployeeDetailViewModel
            {
                User = user,
                History = history
            });
        }
    }

    public class EmployeeDashboardViewModel
    {
        public CurrentWorkStatusDTO Status { get; set; }
        public IEnumerable<WorkHistoryDTO> History { get; set; }
    }

    public class EmployeeDetailViewModel
    {
        public UserDetailDTO User { get; set; }
        public IEnumerable<WorkHistoryDTO> History { get; set; }
    }
}
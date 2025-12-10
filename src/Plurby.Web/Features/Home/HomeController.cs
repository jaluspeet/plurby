using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plurby.Services.Shared;
using System;
using System.Security.Claims;
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
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _service.Query(new UserDetailQuery { Id = userId });

            if (user.Role == UserRole.Manager)
            {
                return RedirectToAction("Index", "Manager");
            }
            else
            {
                return RedirectToAction("Index", "Employee");
            }
        }
    }
}
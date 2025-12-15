using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plurby.Infrastructure;
using Plurby.Services.Shared;
using Plurby.Web.Areas;
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
                ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
                {
                    EmailUtenteCorrente = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };
                return View("ManagerIndex");
            }
            else
            {
                var status = await _service.Query(new CurrentWorkStatusQuery { UserId = user.Id });

                ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
                {
                    EmailUtenteCorrente = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName
                };

                var model = new EmployeeDashboardViewModel
                {
                    Status = status
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
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            var currentUser = await _service.Query(new UserByEmailQuery { Email = email });
            if (currentUser == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
            {
                EmailUtenteCorrente = currentUser.Email,
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName
            };

            var user = await _service.Query(new UserDetailQuery { Id = id });
            var history = await _service.Query(new WorkHistoryQuery { UserId = id });

            var now = DateTime.Now;
            var weeklyHours = CalculateHoursForPeriod(history, now.AddDays(-7), now);
            var monthlyHours = CalculateHoursForPeriod(history, new DateTime(now.Year, now.Month, 1), now);

            return View(new EmployeeDetailViewModel
            {
                User = user,
                History = history,
                WeeklyHours = weeklyHours,
                MonthlyHours = monthlyHours
            });
        }

        private double CalculateHoursForPeriod(IEnumerable<WorkHistoryDTO> history, DateTime startDate, DateTime endDate)
        {
            double totalHours = 0;

            foreach (var entry in history)
            {
                if (entry.Duration.HasValue)
                {
                    var entryStart = entry.StartTime.ToLocalTime();
                    var entryEnd = entry.EndTime?.ToLocalTime() ?? entryStart.Add(entry.Duration.Value);

                    // Check if the work entry overlaps with the period
                    if (entryStart <= endDate && entryEnd >= startDate)
                    {
                        // Calculate the overlap duration
                        var overlapStart = entryStart < startDate ? startDate : entryStart;
                        var overlapEnd = entryEnd > endDate ? endDate : entryEnd;
                        var overlapDuration = overlapEnd - overlapStart;
                        
                        totalHours += overlapDuration.TotalHours;
                    }
                }
            }

            return totalHours;
        }

        public virtual async Task<IActionResult> EmployeeHistory()
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

            var history = await _service.Query(new WorkHistoryQuery { UserId = user.Id });

            ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
            {
                EmailUtenteCorrente = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            var model = new EmployeeHistoryViewModel
            {
                History = history
            };
            return View(model);
        }

        public virtual async Task<IActionResult> ManagerIndex()
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

            if (user.Role != UserRole.Manager)
            {
                return RedirectToAction("Login", "Login");
            }

            ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
            {
                EmailUtenteCorrente = user.Email ?? email,
                FirstName = user.FirstName ?? "",
                LastName = user.LastName ?? ""
            };

            return View("ManagerIndex");
        }

        public virtual async Task<IActionResult> EmployeesManagement()
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

            ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
            {
                EmailUtenteCorrente = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            var employees = await _service.Query(new UsersIndexQuery
            {
                IdCurrentUser = user.Id,
                Role = UserRole.Employee,
                Paging = new Paging { PageSize = 100 }
            });
            return View("EmployeesManagement", employees);
        }

        public virtual async Task<IActionResult> AccountsManagement()
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

            ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
            {
                EmailUtenteCorrente = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            var users = await _service.Query(new UsersIndexQuery
            {
                IdCurrentUser = user.Id,
                Paging = new Paging { PageSize = 100 }
            });
            return View(users);
        }

        [HttpPost]
        public virtual async Task<IActionResult> CreateAccount(string firstName, string lastName, string email, UserRole role, string password)
        {
            var emailCheck = User.Identity?.Name;
            if (string.IsNullOrEmpty(emailCheck))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            await _service.Handle(new AddOrUpdateUserCommand
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                Password = password
            });

            return RedirectToAction(nameof(AccountsManagement));
        }

        [HttpPost]
        public virtual async Task<IActionResult> UpdateAccount(Guid id, string firstName, string lastName, string email, UserRole role, string password)
        {
            var emailCheck = User.Identity?.Name;
            if (string.IsNullOrEmpty(emailCheck))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            await _service.Handle(new AddOrUpdateUserCommand
            {
                Id = id,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                Password = password
            });

            return RedirectToAction(nameof(AccountsManagement));
        }

        [HttpPost]
        public virtual async Task<IActionResult> DeleteAccount(Guid id)
        {
            var emailCheck = User.Identity?.Name;
            if (string.IsNullOrEmpty(emailCheck))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Login");
            }

            await _service.Handle(new DeleteUserCommand { Id = id });

            return RedirectToAction(nameof(AccountsManagement));
        }

        [AllowAnonymous]
        public virtual IActionResult Error(int? statusCode = null)
        {
            if (statusCode == 404)
            {
                return View("NotFound");
            }
            
            return View();
        }
    }

    public class EmployeeDashboardViewModel
    {
        public CurrentWorkStatusDTO Status { get; set; }
    }

    public class EmployeeDetailViewModel
    {
        public UserDetailDTO User { get; set; }
        public IEnumerable<WorkHistoryDTO> History { get; set; }
        public double WeeklyHours { get; set; }
        public double MonthlyHours { get; set; }
    }

    public class EmployeeHistoryViewModel
    {
        public IEnumerable<WorkHistoryDTO> History { get; set; }
    }
}
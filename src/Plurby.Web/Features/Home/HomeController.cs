using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plurby.Infrastructure;
using Plurby.Services.Shared;
using Plurby.Web.Areas;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    LastName = user.LastName,
                    Role = user.Role
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
                    LastName = user.LastName,
                    Role = user.Role
                };

                var history = await _service.Query(new WorkHistoryQuery { UserId = user.Id });
                var today = DateTime.Now.Date;
                var todayEntries = history
                    .Where(e => e.StartTime.ToLocalTime().Date == today)
                    .OrderByDescending(e => e.StartTime)
                    .ToList();

                var model = new EmployeeDashboardViewModel
                {
                    Status = status,
                    TodayEntries = todayEntries
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
                LastName = currentUser.LastName,
                Role = currentUser.Role
            };

            // Store current user role for view conditional rendering
            ViewData["IsManager"] = currentUser.Role == UserRole.Manager;

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
                LastName = user.LastName,
                Role = user.Role
            };

            // Store current user role for view conditional rendering
            ViewData["IsEmployee"] = user.Role == UserRole.Employee;
            ViewData["CurrentUserId"] = user.Id;

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
                LastName = user.LastName ?? "",
                Role = user.Role
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
                LastName = user.LastName,
                Role = user.Role
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
                LastName = user.LastName,
                Role = user.Role
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

        [HttpPost]
        public virtual async Task<IActionResult> EditWorkEntry(Guid workEntryId, DateTime startTime, DateTime? endTime, Guid employeeId, string month = null)
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

            // Only managers can edit work entries
            if (currentUser.Role != UserRole.Manager)
            {
                return Forbid();
            }

            await _service.Handle(new UpdateWorkEntryCommand
            {
                WorkEntryId = workEntryId,
                StartTime = startTime,
                EndTime = endTime
            });

            var redirectParams = new Dictionary<string, object> { { "id", employeeId } };
            if (!string.IsNullOrEmpty(month))
            {
                redirectParams["month"] = month;
            }

            return RedirectToAction(nameof(EmployeeDetail), redirectParams);
        }

        [HttpPost]
        public virtual async Task<IActionResult> ProposeWorkEntryChange(Guid workEntryId, DateTime proposedStartTime, DateTime? proposedEndTime, string month = null)
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

            // Only employees can propose changes (managers can edit directly)
            if (currentUser.Role != UserRole.Employee)
            {
                return Forbid();
            }

            // Verify the work entry belongs to the current user
            var history = await _service.Query(new WorkHistoryQuery { UserId = currentUser.Id });
            if (!history.Any(x => x.Id == workEntryId))
            {
                return Forbid();
            }

            await _service.Handle(new ProposeWorkEntryChangeCommand
            {
                WorkEntryId = workEntryId,
                ProposedByUserId = currentUser.Id,
                ProposedStartTime = proposedStartTime,
                ProposedEndTime = proposedEndTime
            });

            if (!string.IsNullOrEmpty(month))
            {
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            return RedirectToAction(nameof(EmployeeHistory));
        }

        [HttpPost]
        public virtual async Task<IActionResult> AcceptProposal(Guid proposalId, Guid employeeId, string month = null)
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

            // Only managers can accept proposals
            if (currentUser.Role != UserRole.Manager)
            {
                return Forbid();
            }

            await _service.Handle(new AcceptProposalCommand
            {
                ProposalId = proposalId,
                ProcessedByUserId = currentUser.Id
            });

            var redirectParams = new Dictionary<string, object> { { "id", employeeId } };
            if (!string.IsNullOrEmpty(month))
            {
                redirectParams["month"] = month;
            }

            return RedirectToAction(nameof(EmployeeDetail), redirectParams);
        }

        [HttpPost]
        public virtual async Task<IActionResult> RejectProposal(Guid proposalId, Guid employeeId, string month = null)
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

            // Only managers can reject proposals
            if (currentUser.Role != UserRole.Manager)
            {
                return Forbid();
            }

            await _service.Handle(new RejectProposalCommand
            {
                ProposalId = proposalId,
                ProcessedByUserId = currentUser.Id
            });

            var redirectParams = new Dictionary<string, object> { { "id", employeeId } };
            if (!string.IsNullOrEmpty(month))
            {
                redirectParams["month"] = month;
            }

            return RedirectToAction(nameof(EmployeeDetail), redirectParams);
        }

        [AllowAnonymous]
        public virtual IActionResult Error(int? statusCode = null)
        {
            if (statusCode.HasValue && statusCode.Value == 404)
            {
                return View("NotFound");
            }
            
            return View();
        }
    }

    public class EmployeeDashboardViewModel
    {
        public CurrentWorkStatusDTO Status { get; set; }
        public IEnumerable<WorkHistoryDTO> TodayEntries { get; set; } = Enumerable.Empty<WorkHistoryDTO>();
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
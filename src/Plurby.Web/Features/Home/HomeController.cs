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

        /// <summary>
        /// Gets the current authenticated user, or redirects to login if not authenticated or user not found.
        /// </summary>
        /// <returns>The authenticated user, or null if redirecting to login</returns>
        private async Task<(UserDetailDTO user, IActionResult redirectResult)> GetCurrentUserOrRedirect()
        {
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (null, RedirectToAction("Login", "Login"));
            }

            var user = await _service.Query(new UserByEmailQuery { Email = email });
            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (null, RedirectToAction("Login", "Login"));
            }

            return (user, null);
        }

        /// <summary>
        /// Sets the IdentitaViewModel in ViewData for the current user.
        /// </summary>
        private void SetIdentitaViewModel(UserDetailDTO user)
        {
            ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
            {
                EmailUtenteCorrente = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role
            };
        }

        public virtual async Task<IActionResult> Index()
        {
            var (user, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            if (user.Role == UserRole.Manager)
            {
                SetIdentitaViewModel(user);
                return View("ManagerIndex");
            }
            else
            {
                var status = await _service.Query(new CurrentWorkStatusQuery { UserId = user.Id });
                SetIdentitaViewModel(user);

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
            var (user, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            await _service.Handle(new StartWorkCommand { UserId = user.Id });
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public virtual async Task<IActionResult> EndWork()
        {
            var (user, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            await _service.Handle(new EndWorkCommand { UserId = user.Id });
            return RedirectToAction(nameof(Index));
        }

        public virtual async Task<IActionResult> EmployeeDetail(Guid id)
        {
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            SetIdentitaViewModel(currentUser);
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
            var (user, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            var history = await _service.Query(new WorkHistoryQuery { UserId = user.Id });
            SetIdentitaViewModel(user);

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
            var (user, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            if (user.Role != UserRole.Manager)
            {
                return RedirectToAction("Login", "Login");
            }

            SetIdentitaViewModel(user);
            return View("ManagerIndex");
        }

        public virtual async Task<IActionResult> EmployeesManagement()
        {
            var (user, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            SetIdentitaViewModel(user);

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
            var (user, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            SetIdentitaViewModel(user);

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
            var (_, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

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
            var (_, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

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
            var (_, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            await _service.Handle(new DeleteUserCommand { Id = id });

            return RedirectToAction(nameof(AccountsManagement));
        }

        [HttpPost]
        public virtual async Task<IActionResult> EditWorkEntry(Guid workEntryId, DateTime startTime, DateTime? endTime, Guid employeeId, string month = null)
        {
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

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
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            // Only employees can propose changes (managers can edit directly)
            if (currentUser.Role != UserRole.Employee)
            {
                return Forbid();
            }

            // Verify the work entry belongs to the current user
            var history = await _service.Query(new WorkHistoryQuery { UserId = currentUser.Id });
            var workEntry = history.FirstOrDefault(x => x.Id == workEntryId);
            if (workEntry == null)
            {
                return Forbid();
            }

            // Only allow proposing changes to finished entries (with end date)
            if (!workEntry.EndTime.HasValue)
            {
                TempData["Error"] = "È possibile proporre modifiche solo per voci di lavoro completate (con ora di fine).";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            // Validate proposed dates
            if (proposedStartTime > DateTime.Now)
            {
                TempData["Error"] = "L'ora di inizio proposta non può essere nel futuro.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            if (proposedEndTime.HasValue && proposedStartTime >= proposedEndTime.Value)
            {
                TempData["Error"] = "L'ora di inizio deve essere precedente all'ora di fine.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            // End time is now required for proposals
            if (!proposedEndTime.HasValue)
            {
                TempData["Error"] = "L'ora di fine è obbligatoria per le proposte di modifica.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
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

            TempData["Success"] = "Proposta di modifica inviata con successo. In attesa di approvazione.";
            return RedirectToAction(nameof(EmployeeHistory));
        }

        [HttpPost]
        public virtual async Task<IActionResult> ProposeNewWorkEntry(DateTime startTime, DateTime endTime, string month = null)
        {
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            // Only employees can propose new entries (managers can add directly)
            if (currentUser.Role != UserRole.Employee)
            {
                return Forbid();
            }

            // Validate proposed dates
            if (startTime > DateTime.Now)
            {
                TempData["Error"] = "L'ora di inizio non può essere nel futuro.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            if (startTime >= endTime)
            {
                TempData["Error"] = "L'ora di inizio deve essere precedente all'ora di fine.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            // Use service to create new work entry proposal
            await _service.Handle(new ProposeNewWorkEntryCommand
            {
                ProposedByUserId = currentUser.Id,
                ProposedStartTime = startTime,
                ProposedEndTime = endTime
            });

            TempData["Success"] = "Proposta di nuova voce di lavoro inviata con successo. In attesa di approvazione.";

            if (!string.IsNullOrEmpty(month))
            {
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            return RedirectToAction(nameof(EmployeeHistory));
        }

        [HttpPost]
        public virtual async Task<IActionResult> EditNewProposal(Guid editProposalId, DateTime startTime, DateTime endTime, string month = null)
        {
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            // Only employees can edit their own proposals
            if (currentUser.Role != UserRole.Employee)
            {
                return Forbid();
            }

            // Find proposal using service
            var proposals = await _service.Query(new WorkEntryProposalsQuery { UserId = currentUser.Id });
            var proposal = proposals.FirstOrDefault(x => x.Id == editProposalId && x.Status == ProposalStatus.Pending);

            if (proposal == null)
            {
                TempData["Error"] = "Proposta non trovata o già processata.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            // Validate dates
            if (startTime > DateTime.Now)
            {
                TempData["Error"] = "L'ora di inizio non può essere nel futuro.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            if (startTime >= endTime)
            {
                TempData["Error"] = "L'ora di inizio deve essere precedente all'ora di fine.";
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            // Update proposal using service
            await _service.Handle(new UpdateNewProposalCommand
            {
                ProposalId = editProposalId,
                ProposedStartTime = startTime,
                ProposedEndTime = endTime
            });

            TempData["Success"] = "Proposta aggiornata con successo. In attesa di approvazione.";

            if (!string.IsNullOrEmpty(month))
            {
                return RedirectToAction(nameof(EmployeeHistory), new { month = month });
            }

            return RedirectToAction(nameof(EmployeeHistory));
        }

        [HttpPost]
        public virtual async Task<IActionResult> AddNewWorkEntry(Guid employeeId, DateTime startTime, DateTime endTime, string month = null)
        {
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

            // Only managers can add new work entries directly
            if (currentUser.Role != UserRole.Manager)
            {
                return Forbid();
            }

            // Validate dates
            if (startTime >= endTime)
            {
                TempData["Error"] = "L'ora di inizio deve essere precedente all'ora di fine.";
                return RedirectToAction(nameof(EmployeeDetail), new { id = employeeId, month = month });
            }

            // Use service to create new work entry
            await _service.Handle(new AddNewWorkEntryCommand
            {
                UserId = employeeId,
                StartTime = startTime,
                EndTime = endTime
            });

            TempData["Success"] = "Nuova voce di lavoro aggiunta con successo.";

            return RedirectToAction(nameof(EmployeeDetail), new { id = employeeId, month = month });
        }

        [HttpPost]
        public virtual async Task<IActionResult> AcceptProposal(Guid proposalId, Guid employeeId, string month = null)
        {
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

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
            var (currentUser, redirectResult) = await GetCurrentUserOrRedirect();
            if (redirectResult != null)
                return redirectResult;

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
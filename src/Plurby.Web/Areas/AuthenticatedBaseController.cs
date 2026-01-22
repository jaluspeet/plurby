using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Plurby.Infrastructure;
using Plurby.Services.Shared;
using Plurby.Web.Infrastructure;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Plurby.Web.Areas
{
    [Authorize]
    [Alerts]
    [ModelStateToTempData]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public partial class AuthenticatedBaseController : Controller
    {
        protected readonly SharedService _sharedService;

        public AuthenticatedBaseController(SharedService sharedService)
        {
            _sharedService = sharedService;
        }

        protected IdentitaViewModel Identita
        {
            get
            {
                return (IdentitaViewModel)ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY];
            }
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.HttpContext != null && context.HttpContext.User != null && context.HttpContext.User.Identity.IsAuthenticated)
            {
                var email = context.HttpContext.User.Claims.Where(x => x.Type == ClaimTypes.Email).First().Value;
                var user = await _sharedService.Query(new UserByEmailQuery { Email = email });
                
                ViewData[IdentitaViewModel.VIEWDATA_IDENTITACORRENTE_KEY] = new IdentitaViewModel
                {
                    EmailUtenteCorrente = email,
                    FirstName = user?.FirstName ?? "",
                    LastName = user?.LastName ?? "",
                    Role = user?.Role ?? UserRole.Employee
                };
            }
            else
            {
                await HttpContext.SignOutAsync();
                SignOut();

                context.Result = new RedirectResult(context.HttpContext.Request.GetEncodedUrl());
                Alerts.AddError(this, "L'utente non possiede i diritti per visualizzare la risorsa richiesta");
                return;
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}

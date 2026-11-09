using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Services;

public class RequireLoginAttribute : ActionFilterAttribute
{
    public string? Role { get; set; }

    public override void OnActionExecuting(ActionExecutingContext ctx)
    {
        var user = ctx.HttpContext.Session.GetCurrentUser();
        if (user is null)
        {
            ctx.Result = new RedirectToActionResult("Login", "Account",
                new { returnUrl = ctx.HttpContext.Request.Path });
        }
        else if (Role != null && user.Role != Role)
        {
            ctx.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
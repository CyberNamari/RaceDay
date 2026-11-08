using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class BaseController : Controller
{
    // If the API says 401, the API session expired: clear ours and send them to login.
    protected IActionResult? SessionExpired<T>(ApiResult<T> result)
    {
        if (!result.Unauthorized) return null;
        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }
}
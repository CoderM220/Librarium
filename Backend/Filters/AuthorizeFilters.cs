using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Librarium.Filters
{
    // Protects admin pages
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                .Any(em => em is AllowAnonymousAttribute);
            if (hasAllowAnonymous) return;

            if (context.HttpContext.Session.GetInt32("AdminId") == null)
            {
                context.Result = new RedirectResult("/Auth/AdminLogin");
            }
        }
    }

    // Protects student pages
    public class StudentAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Session.GetInt32("StudentId") == null)
            {
                var request = context.HttpContext.Request;
                var isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                             (request.ContentType?.Contains("application/x-www-form-urlencoded") == true &&
                              request.Method == "POST");

                if (isAjax || request.Method == "POST")
                {
                    context.Result = new JsonResult(new { success = false, message = "Session expired. Please log in again.", expired = true })
                    {
                        StatusCode = 401
                    };
                }
                else
                {
                    context.Result = new RedirectResult("/Auth/StudentLogin");
                }
            }
        }
    }
}
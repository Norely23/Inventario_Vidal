using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inventario_Vidal.Filters
{
    public class LoginRequeridoAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.Any(x => x is PermitirAccesoPublicoAttribute))
                return;

            if (context.HttpContext.Session.GetInt32("IdUsuario").HasValue)
                return;

            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
        }
    }
}

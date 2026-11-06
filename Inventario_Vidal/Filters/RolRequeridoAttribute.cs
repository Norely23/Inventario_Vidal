using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Inventario_Vidal.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class RolRequeridoAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _rolesPermitidos;
        public RolRequeridoAttribute(params string[] roles)
        {
            _rolesPermitidos = roles.Select(r => r.Trim()).Where(r => r.Length > 0).ToArray();
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is PermitirAccesoPublicoAttribute)) return;
            var id = context.HttpContext.Session.GetInt32("IdUsuario");
            if (!id.HasValue || id.Value <= 0)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            var rol = context.HttpContext.Session.GetString("NombreRol")?.Trim();
            if (string.IsNullOrWhiteSpace(rol) || !_rolesPermitidos.Contains(rol, StringComparer.OrdinalIgnoreCase))
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

using Inventario_Vidal.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PermisoRequeridoAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _codigo;
        public PermisoRequeridoAttribute(string codigo)
        {
            _codigo = codigo?.Trim() ?? throw new ArgumentNullException(nameof(codigo));
            if (_codigo.Length == 0) throw new ArgumentException("El permiso es obligatorio.", nameof(codigo));
        }
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor.EndpointMetadata.Any(m => m is PermitirAccesoPublicoAttribute)) return;
            var id = context.HttpContext.Session.GetInt32("IdUsuario");
            if (!id.HasValue || id.Value <= 0)
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<FabricacionesVidalEirlBdContext>();
            // No confiar solo en el nombre del rol almacenado en Session.
            var permitido = await db.Usuarios.AsNoTracking().AnyAsync(u =>
                u.IdUsuario == id.Value && u.Activo && u.IdRolNavigation.Activo &&
                u.IdRolNavigation.IdPermisos.Any(permiso => permiso.Activo && permiso.Codigo == _codigo));
            if (!permitido) context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}

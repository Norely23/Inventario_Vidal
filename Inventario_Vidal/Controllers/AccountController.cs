using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Helpers;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _accessor;

        public AccountController(IAuthService authService, IHttpContextAccessor accessor)
        {
            _authService = authService;
            _accessor = accessor;
        }

        [HttpGet]
        public async Task<IActionResult> Perfil()
        {
            var usuarioId = SesionHelper.GetUsuarioId(_accessor);

            if (!usuarioId.HasValue)
                return RedirectToAction("Login", "Auth");

            var perfil = await _authService.ObtenerPerfilAsync(usuarioId.Value);

            if (perfil == null)
                return RedirectToAction("Login", "Auth");

            return View(perfil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perfil(PerfilUsuarioDto model)
        {
            var usuarioId = SesionHelper.GetUsuarioId(_accessor);

            if (!usuarioId.HasValue)
                return RedirectToAction("Login", "Auth");

            if (usuarioId.Value != model.IdUsuario)
                return Forbid();

            if (!ModelState.IsValid)
            {
                var actual = await _authService.ObtenerPerfilAsync(usuarioId.Value);

                if (actual != null)
                {
                    model.NombreUsuario = actual.NombreUsuario;
                    model.NombreRol = actual.NombreRol;
                    model.FechaCreacion = actual.FechaCreacion;
                    model.UltimoAcceso = actual.UltimoAcceso;
                }

                return View(model);
            }

            var result = await _authService.ActualizarPerfilAsync(usuarioId.Value, model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);

                var actual = await _authService.ObtenerPerfilAsync(usuarioId.Value);

                if (actual != null)
                {
                    model.NombreUsuario = actual.NombreUsuario;
                    model.NombreRol = actual.NombreRol;
                    model.FechaCreacion = actual.FechaCreacion;
                    model.UltimoAcceso = actual.UltimoAcceso;
                }

                return View(model);
            }

            TempData["MensajeExito"] = result.Message;
            return RedirectToAction(nameof(Perfil));
        }

        [HttpGet]
        public IActionResult CambiarPassword()
        {
            return View(new CambiarPasswordDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordDto request)
        {
            if (!ModelState.IsValid)
                return View(request);

            var usuarioId = SesionHelper.GetUsuarioId(_accessor);

            if (!usuarioId.HasValue)
                return RedirectToAction("Login", "Auth");

            var result = await _authService.CambiarPasswordAsync(usuarioId.Value, request);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(request);
            }

            await _authService.CerrarTodasSesionesAsync(usuarioId.Value);
            Response.Cookies.Delete("TokenSesion", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"
            });

            HttpContext.Session.Clear();
            TempData["MensajeExito"] = "Contraseña actualizada. Inicia sesión nuevamente.";
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public async Task<IActionResult> MisSesiones()
        {
            var usuarioId = SesionHelper.GetUsuarioId(_accessor);

            if (!usuarioId.HasValue)
                return RedirectToAction("Login", "Auth");

            var tokenActual = HttpContext.Session.GetString("TokenSesion");
            var sesiones = await _authService.ObtenerSesionesAsync(usuarioId.Value, tokenActual);

            return View(sesiones);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarSesionRemota(long tokenId)
        {
            var usuarioId = SesionHelper.GetUsuarioId(_accessor);

            if (!usuarioId.HasValue)
                return Json(new { success = false, message = "No autorizado" });

            var tokenActual = HttpContext.Session.GetString("TokenSesion");
            var result = await _authService.CerrarSesionRemotaAsync(usuarioId.Value, tokenId, tokenActual);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarTodasSesionesRemotas()
        {
            var usuarioId = SesionHelper.GetUsuarioId(_accessor);

            if (!usuarioId.HasValue)
                return Json(new { success = false, message = "No autorizado" });

            var tokenActual = HttpContext.Session.GetString("TokenSesion");
            var result = await _authService.CerrarSesionesRemotasAsync(usuarioId.Value, tokenActual);

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

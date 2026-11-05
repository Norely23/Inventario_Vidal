using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Helpers;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmpresaService _empresaService;
        private readonly IHttpContextAccessor _accessor;

        public AuthController(
            IAuthService authService,
            IEmpresaService empresaService,
            IHttpContextAccessor accessor)
        {
            _authService = authService;
            _empresaService = empresaService;
            _accessor = accessor;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (SesionHelper.TieneSesion(_accessor))
            {
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return LocalRedirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            await CargarIdentidadEmpresaAsync();
            return View(new LoginRequestDto { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                await CargarIdentidadEmpresaAsync();
                return View(request);
            }

            request.Ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.UserAgent = Request.Headers["User-Agent"].ToString();

            var response = await _authService.LoginAsync(request);

            if (!response.Success)
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "No se pudo iniciar sesión");
                await CargarIdentidadEmpresaAsync();
                return View(request);
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"
            };

            Response.Cookies.Delete("TokenSesion", cookieOptions);

            if (request.RecordarSesion && !string.IsNullOrWhiteSpace(response.Token))
            {
                Response.Cookies.Append("TokenSesion", response.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    IsEssential = true,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    MaxAge = TimeSpan.FromDays(30)
                });
            }

            var destino = !string.IsNullOrWhiteSpace(request.ReturnUrl) && Url.IsLocalUrl(request.ReturnUrl)
                ? request.ReturnUrl
                : Url.Action("Index", "Home") ?? "/";

            return RedirectToAction(nameof(Bienvenida), new { returnUrl = destino });
        }

        [LoginRequerido]
        [HttpGet]
        public async Task<IActionResult> Bienvenida(string? returnUrl = null)
        {
            var destino = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : Url.Action("Index", "Home") ?? "/";

            var empresa = await _empresaService.GetConfiguracionAsync();

            ViewBag.EmpresaNombre = string.IsNullOrWhiteSpace(empresa?.NombreEmpresa)
                ? "VIDAL EIRL"
                : empresa.NombreEmpresa;

            ViewBag.Usuario = HttpContext.Session.GetString("NombreCompleto")
                ?? HttpContext.Session.GetString("NombreUsuario")
                ?? "Usuario";

            ViewBag.Rol = HttpContext.Session.GetString("NombreRol") ?? string.Empty;
            ViewBag.Destino = destino;

            return View();
        }

        [LoginRequerido]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Session.GetString("TokenSesion");

            if (!string.IsNullOrWhiteSpace(token))
                await _authService.CerrarSesionAsync(token);

            BorrarCookiePersistente();
            HttpContext.Session.Clear();

            return RedirectToAction(nameof(Login));
        }

        private async Task CargarIdentidadEmpresaAsync()
        {
            var empresa = await _empresaService.GetConfiguracionAsync();
            ViewBag.EmpresaNombre = string.IsNullOrWhiteSpace(empresa?.NombreEmpresa)
                ? "VIDAL EIRL"
                : empresa.NombreEmpresa;
        }

        private void BorrarCookiePersistente()
        {
            Response.Cookies.Delete("TokenSesion", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Path = "/"
            });
        }
    }
}

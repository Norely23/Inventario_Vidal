using System.Security.Cryptography;
using Inventario_Vidal.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace Inventario_Vidal.Controllers
{
    /// <summary>
    /// Conecta temporalmente el formulario de Materiales de la PC con el lector
    /// de códigos de barras del celular. No escribe materiales ni modifica stock.
    /// </summary>
    [LoginRequerido]
    public sealed class CodigoBarrasMovilController : Controller
    {
        private static readonly TimeSpan DuracionSesion = TimeSpan.FromMinutes(5);
        private readonly IMemoryCache _cache;

        public CodigoBarrasMovilController(IMemoryCache cache)
        {
            _cache = cache;
        }

        private sealed class SesionLectura
        {
            public required int IdUsuario { get; init; }
            public required DateTime VenceUtc { get; init; }
            public string? Codigo { get; set; }
            public object Sincronizacion { get; } = new();
        }

        private static string Clave(string token) => $"Materiales:CodigoBarrasMovil:{token}";

        private bool TryGetSesion(string? token, out SesionLectura? sesion)
        {
            sesion = null;
            if (string.IsNullOrWhiteSpace(token) || token.Length != 43 ||
                !token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
                return false;

            if (!_cache.TryGetValue(Clave(token), out SesionLectura? encontrada) ||
                encontrada is null || encontrada.VenceUtc <= DateTime.UtcNow)
                return false;

            sesion = encontrada;
            return true;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearSesion()
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario is null or <= 0)
                return Unauthorized(new { success = false, message = "Inicia sesión nuevamente." });

            // Un QR con localhost apuntaría al propio celular, no a la PC.
            if (Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                Request.Host.Host is "127.0.0.1" or "::1")
                return BadRequest(new
                {
                    success = false,
                    message = "Abre VIDAL desde la IP de la PC en la red (por ejemplo, https://192.168.0.114:5051), no desde localhost."
                });

            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var vence = DateTime.UtcNow.Add(DuracionSesion);
            _cache.Set(Clave(token), new SesionLectura { IdUsuario = idUsuario.Value, VenceUtc = vence },
                new MemoryCacheEntryOptions { AbsoluteExpiration = new DateTimeOffset(vence) });

            var url = Url.Action("Movil", "CodigoBarrasMovil", new { token }, Request.Scheme, Request.Host.Value);
            if (string.IsNullOrWhiteSpace(url))
            {
                _cache.Remove(Clave(token));
                return StatusCode(500, new { success = false, message = "No se pudo generar el enlace del escáner." });
            }

            Response.Headers["Cache-Control"] = "no-store";
            return Json(new { success = true, token, url, segundos = (int)DuracionSesion.TotalSeconds });
        }

        [HttpGet]
        [PermitirAccesoPublico]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Movil(string? token)
        {
            Response.Headers["Referrer-Policy"] = "no-referrer";
            ViewBag.Token = token ?? string.Empty;
            ViewBag.SesionValida = TryGetSesion(token, out var sesion) && sesion is not null && sesion.Codigo is null;
            return View("~/Views/Materiales/Movil.cshtml");
        }

        [HttpPost]
        [PermitirAccesoPublico]
        [ValidateAntiForgeryToken]
        public IActionResult EnviarCodigo(string? token, string? codigo)
        {
            Response.Headers["Cache-Control"] = "no-store";
            if (!TryGetSesion(token, out var sesion) || sesion is null)
                return BadRequest(new { success = false, message = "El QR expiró. Genera uno nuevo desde la computadora." });

            codigo = codigo?.Trim();
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length > 50 || codigo.Any(char.IsControl))
                return BadRequest(new { success = false, message = "El código debe tener entre 1 y 50 caracteres válidos." });

            lock (sesion.Sincronizacion)
            {
                if (sesion.Codigo is not null)
                    return Conflict(new { success = false, message = "Esta sesión ya recibió un código. Abre un nuevo QR para otra lectura." });

                sesion.Codigo = codigo;
            }

            return Json(new { success = true, message = "Código enviado a la computadora. Ya puedes continuar allí." });
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Consultar(string? token)
        {
            if (!TryGetSesion(token, out var sesion) || sesion is null)
                return Json(new { success = false, message = "El QR expiró o se cerró." });

            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario != sesion.IdUsuario)
                return Unauthorized(new { success = false, message = "No tienes acceso a esta sesión." });

            string? codigo;
            lock (sesion.Sincronizacion)
                codigo = sesion.Codigo;

            return Json(new
            {
                success = true,
                completada = codigo is not null,
                codigo,
                segundos = Math.Max(0, (int)(sesion.VenceUtc - DateTime.UtcNow).TotalSeconds)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cerrar(string? token)
        {
            if (TryGetSesion(token, out var sesion) && sesion is not null &&
                HttpContext.Session.GetInt32("IdUsuario") == sesion.IdUsuario)
                _cache.Remove(Clave(token!));

            return Json(new { success = true });
        }
    }
}
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using InventarioVidal.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    [RolRequerido("Administrador")]
    public class TiposCambioController : Controller
    {
        private readonly IEmpresaService _empresaService;
        private readonly ITipoCambioService _tipoCambioService;
        private readonly ILogger<TiposCambioController> _logger;

        public TiposCambioController(
            IEmpresaService empresaService,
            ITipoCambioService tipoCambioService,
            ILogger<TiposCambioController> logger)
        {
            _empresaService = empresaService;
            _tipoCambioService = tipoCambioService;
            _logger = logger;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");

                // Actualiza el tipo de cambio del día solo si aún no existe.
                var actual = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                var tiposCambio = await _empresaService.GetTiposCambioAsync(desde, hasta);
                var ultimo = await _empresaService.GetUltimoTipoCambioAsync();

                ViewBag.Ultimo = ultimo;
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                ViewBag.EsAutomatico = true;
                ViewBag.TipoCambioActual = actual.Valor;
                ViewBag.FechaTipoCambioActual = actual.Fecha;
                ViewBag.VieneDeCache = actual.VieneDeCache;
                ViewBag.VieneDeFallback = actual.VieneDeFallback;

                return View(tiposCambio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar tipos de cambio");
                TempData["MensajeError"] = "Error al cargar los tipos de cambio.";

                ViewBag.EsAutomatico = true;
                return View(new List<TipoCambioDto>());
            }
        }

        // ============================================================
        // CREAR - GET
        // Registro manual opcional
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var actual = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                await CargarDatosCrearAsync();

                var model = new TipoCambioDto
                {
                    Fecha = DateOnly.FromDateTime(DateTime.Now),
                    MonedaOrigen = "USD",
                    MonedaDestino = "PEN",
                    Valor = actual.Valor
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de creación de tipo de cambio");
                TempData["MensajeError"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // CREAR - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(TipoCambioDto model)
        {
            NormalizarModelo(model);

            if (!ModelState.IsValid)
            {
                await CargarDatosCrearAsync();
                return View(model);
            }

            try
            {
                if (model.Valor <= 0)
                {
                    ModelState.AddModelError(
                        nameof(model.Valor),
                        "El tipo de cambio debe ser mayor que cero.");

                    await CargarDatosCrearAsync();
                    return View(model);
                }

                var existe = await _empresaService.ExisteTipoCambioAsync(
                    model.Fecha,
                    model.MonedaOrigen,
                    model.MonedaDestino);

                if (existe)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Ya existe un tipo de cambio para esa fecha y par de monedas.");

                    await CargarDatosCrearAsync();
                    return View(model);
                }

                var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

                var result = await _empresaService.RegistrarTipoCambioAsync(
                    model,
                    usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["MensajeError"] = result.Message;
                await CargarDatosCrearAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar tipo de cambio");
                TempData["MensajeError"] = "Error al registrar el tipo de cambio.";

                await CargarDatosCrearAsync();
                return View(model);
            }
        }

        // ============================================================
        // OBTENER ÚLTIMO TIPO DE CAMBIO
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetUltimoTipoCambio()
        {
            try
            {
                var ultimo = await _empresaService.GetUltimoTipoCambioAsync();

                if (ultimo == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No hay tipos de cambio registrados."
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        ultimo.IdTipoCambio,
                        ultimo.Fecha,
                        ultimo.MonedaOrigen,
                        ultimo.MonedaDestino,
                        ultimo.Valor,
                        ultimo.UsuarioNombre,
                        ultimo.FechaRegistro,
                        ultimo.FechaFormateada
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener último tipo de cambio");

                return Json(new
                {
                    success = false,
                    message = "Error al obtener el tipo de cambio."
                });
            }
        }

        // ============================================================
        // VERIFICAR FECHA
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> VerificarFecha(
            DateOnly fecha,
            string monedaOrigen = "USD",
            string monedaDestino = "PEN")
        {
            try
            {
                monedaOrigen = NormalizarMoneda(monedaOrigen, "USD");
                monedaDestino = NormalizarMoneda(monedaDestino, "PEN");

                var existe = await _empresaService.ExisteTipoCambioAsync(
                    fecha,
                    monedaOrigen,
                    monedaDestino);

                return Json(new
                {
                    exists = existe
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar fecha de tipo de cambio");

                return Json(new
                {
                    exists = false,
                    error = true
                });
            }
        }

        // ============================================================
        // OBTENER HISTORIAL POR RANGO
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetTiposCambioFecha(
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            try
            {
                var tipos = await _empresaService.GetTiposCambioAsync(desde, hasta);

                return Json(new
                {
                    success = true,
                    data = tipos.Select(t => new
                    {
                        t.IdTipoCambio,
                        t.Fecha,
                        t.MonedaOrigen,
                        t.MonedaDestino,
                        t.Valor,
                        t.UsuarioNombre,
                        t.FechaRegistro,
                        t.FechaFormateada
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de cambio por fecha");

                return Json(new
                {
                    success = false,
                    message = "Error al obtener los datos."
                });
            }
        }

        // ============================================================
        // OBTENER TIPO DE CAMBIO POR FECHA
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetTipoCambioByDate(
            DateOnly fecha,
            string monedaOrigen = "USD",
            string monedaDestino = "PEN")
        {
            try
            {
                monedaOrigen = NormalizarMoneda(monedaOrigen, "USD");
                monedaDestino = NormalizarMoneda(monedaDestino, "PEN");

                var desde = fecha.ToDateTime(TimeOnly.MinValue);
                var hasta = fecha.ToDateTime(TimeOnly.MaxValue);

                var tipos = await _empresaService.GetTiposCambioAsync(desde, hasta);

                var tipo = tipos.FirstOrDefault(t =>
                    t.Fecha == fecha &&
                    string.Equals(
                        t.MonedaOrigen,
                        monedaOrigen,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        t.MonedaDestino,
                        monedaDestino,
                        StringComparison.OrdinalIgnoreCase));

                if (tipo == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se encontró tipo de cambio para esta fecha."
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        tipo.IdTipoCambio,
                        tipo.Fecha,
                        tipo.MonedaOrigen,
                        tipo.MonedaDestino,
                        tipo.Valor,
                        tipo.UsuarioNombre,
                        tipo.FechaRegistro,
                        tipo.FechaFormateada
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener tipo de cambio para la fecha {Fecha}",
                    fecha);

                return Json(new
                {
                    success = false,
                    message = "Error al obtener el tipo de cambio."
                });
            }
        }

        // ============================================================
        // OBTENER TIPO DE CAMBIO ACTUAL
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetActual()
        {
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var actual = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        actual.IdTipoCambio,
                        actual.Valor,
                        actual.Compra,
                        actual.Venta,
                        fecha = actual.Fecha.ToString("dd/MM/yyyy"),
                        actual.VieneDeCache,
                        actual.VieneDeFallback
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el tipo de cambio actual");

                return Json(new
                {
                    success = false,
                    message = "No se pudo obtener el tipo de cambio actual."
                });
            }
        }

        // ============================================================
        // MÉTODOS PRIVADOS
        // ============================================================

        private async Task CargarDatosCrearAsync()
        {
            var ultimo = await _empresaService.GetUltimoTipoCambioAsync();

            var ultimos = await _empresaService.GetTiposCambioAsync(
                DateTime.Now.AddMonths(-3),
                DateTime.Now);

            ViewBag.Ultimo = ultimo;
            ViewBag.Ultimos = ultimos.ToList();
            ViewBag.EsAutomatico = true;
        }

        private static void NormalizarModelo(TipoCambioDto model)
        {
            model.MonedaOrigen = NormalizarMoneda(
                model.MonedaOrigen,
                "USD");

            model.MonedaDestino = NormalizarMoneda(
                model.MonedaDestino,
                "PEN");
        }

        private static string NormalizarMoneda(
            string? moneda,
            string valorPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(moneda))
                return valorPorDefecto;

            return moneda.Trim().ToUpperInvariant();
        }
    }
}
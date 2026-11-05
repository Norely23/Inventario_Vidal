using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class KardexController : Controller
    {
        private readonly IKardexService _kardexService;
        private readonly IMaterialService _materialService;
        private readonly ILogger<KardexController> _logger;

        public KardexController(
            IKardexService kardexService,
            IMaterialService materialService,
            ILogger<KardexController> logger)
        {
            _kardexService = kardexService;
            _materialService = materialService;
            _logger = logger;
        }

        [HttpGet]
        [PermisoRequerido("KARDEX.VER")]
        public async Task<IActionResult> Index(
            KardexFiltroDto? filtro = null)
        {
            try
            {
                filtro ??= new KardexFiltroDto();

                var movimientos =
                    await _kardexService
                        .GetMovimientosAsync(filtro);

                var materiales =
                    await _materialService
                        .GetMaterialesSelectAsync();

                var tiposMovimiento =
                    await _kardexService
                        .GetTiposMovimientoAsync();

                ViewBag.Materiales =
                    materiales;

                ViewBag.TiposMovimiento =
                    tiposMovimiento;

                ViewBag.Filtro =
                    filtro;

                return View(movimientos);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar movimientos de kardex");

                TempData["MensajeError"] =
                    "Error al cargar los movimientos";

                return View(
                    new List<KardexMovimientoDto>());
            }
        }

        [HttpGet]
        [PermisoRequerido("KARDEX.VER")]
        public async Task<IActionResult> Detalles(long id)
        {
            try
            {
                var movimiento =
                    await _kardexService
                        .GetMovimientoByIdAsync(id);

                if (movimiento == null)
                {
                    TempData["MensajeError"] =
                        "Movimiento no encontrado";

                    return RedirectToAction(
                        nameof(Index));
                }

                return View(movimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar detalles del movimiento {Id}",
                    id);

                TempData["MensajeError"] =
                    "Error al cargar los detalles";

                return RedirectToAction(
                    nameof(Index));
            }
        }

        [HttpGet]
        [PermisoRequerido("KARDEX.VER")]
        public async Task<IActionResult> GetStockActual(
            int idMaterial)
        {
            try
            {
                var stock =
                    await _kardexService
                        .GetStockActualAsync(
                            idMaterial);

                return Json(
                    new
                    {
                        success = true,
                        stock
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener stock actual del material {IdMaterial}",
                    idMaterial);

                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Error al obtener el stock"
                    });
            }
        }

        [HttpGet]
        [PermisoRequerido("KARDEX.VER")]
        public async Task<IActionResult> GetMovimientosByMaterial(
            int idMaterial,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            try
            {
                var movimientos =
                    await _kardexService
                        .GetMovimientosByMaterialAsync(
                            idMaterial,
                            desde,
                            hasta);

                return Json(
                    new
                    {
                        success = true,

                        data =
                            movimientos.Select(
                                m => new
                                {
                                    m.IdMovimiento,
                                    m.FechaMovimiento,
                                    m.TipoMovimiento,
                                    m.TipoBadge,
                                    m.Cantidad,
                                    m.StockAnterior,
                                    m.StockPosterior,
                                    m.Observacion,
                                    m.Usuario
                                })
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener movimientos por material");

                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Error al obtener movimientos"
                    });
            }
        }

        [HttpGet]
        [PermisoRequerido("KARDEX.VER")]
        public async Task<IActionResult> GetResumenMovimientos(
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            try
            {
                var resumen =
                    await _kardexService
                        .GetResumenMovimientosAsync(
                            desde,
                            hasta);

                return Json(
                    new
                    {
                        success = true,
                        data = resumen
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener resumen de movimientos");

                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Error al obtener resumen"
                    });
            }
        }

        [HttpGet]
        [PermisoRequerido("KARDEX.VER")]
        public async Task<IActionResult> GetTiposMovimiento()
        {
            try
            {
                var tipos =
                    await _kardexService
                        .GetTiposMovimientoAsync();

                return Json(
                    new
                    {
                        success = true,

                        data =
                            tipos.Select(
                                t => new
                                {
                                    t.IdTipoMovimiento,
                                    t.Nombre,
                                    t.Signo,
                                    t.SignoTexto,
                                    t.BadgeColor
                                })
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener tipos de movimiento");

                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Error al obtener tipos"
                    });
            }
        }

        [HttpGet]
        [PermisoRequerido("KARDEX.VER")]
        public async Task<IActionResult> GetHistorialMaterial(
            int idMaterial)
        {
            try
            {
                var historial =
                    await _kardexService
                        .GetHistorialMaterialAsync(
                            idMaterial);

                return Json(
                    new
                    {
                        success = true,

                        data =
                            historial.Select(
                                m => new
                                {
                                    m.IdMovimiento,
                                    m.FechaMovimiento,
                                    m.TipoMovimiento,
                                    m.TipoBadge,
                                    m.Cantidad,
                                    m.StockAnterior,
                                    m.StockPosterior,
                                    m.Observacion,
                                    m.Usuario
                                })
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener historial del material");

                return Json(
                    new
                    {
                        success = false,
                        message =
                            "Error al obtener historial"
                    });
            }
        }
    }
}
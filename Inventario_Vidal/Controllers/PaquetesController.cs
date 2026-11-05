using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class PaquetesController : Controller
    {
        private readonly IPaqueteService _paqueteService;
        private readonly IMaterialService _materialService;
        private readonly IReporteService _reporteService;
        private readonly IReportePdfService _reportePdfService;
        private readonly ILogger<PaquetesController> _logger;

        public PaquetesController(
            IPaqueteService paqueteService,
            IMaterialService materialService,
            IReporteService reporteService,
            IReportePdfService reportePdfService,
            ILogger<PaquetesController> logger)
        {
            _paqueteService = paqueteService;
            _materialService = materialService;
            _reporteService = reporteService;
            _reportePdfService = reportePdfService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO DE PAQUETES
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> Index(PaqueteFiltroDto? filtro = null)
        {
            try
            {
                filtro ??= new PaqueteFiltroDto();
                var paquetes = await _paqueteService.GetPaquetesAsync(filtro);
                var materiales = await _materialService.GetMaterialesSelectAsync();

                ViewBag.Materiales = materiales;
                ViewBag.Filtro = filtro;
                ViewBag.TotalPaquetes = paquetes.Count();
                ViewBag.Sellados = paquetes.Count(p => p.Estado == "Sellado");
                ViewBag.Abiertos = paquetes.Count(p => p.Estado == "Abierto");
                ViewBag.Agotados = paquetes.Count(p => p.Estado == "Agotado");

                return View(paquetes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar paquetes");
                TempData["MensajeError"] = "Error al cargar los paquetes";
                return View(new List<PaqueteListDto>());
            }
        }

        // ================================================================
        // DETALLES DE UN PAQUETE
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> Detalles(long id)
        {
            try
            {
                var paquete = await _paqueteService.GetPaqueteByIdAsync(id);
                if (paquete == null)
                {
                    TempData["MensajeError"] = "Paquete no encontrado";
                    return RedirectToAction("Index");
                }

                return View(paquete);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del paquete {Id}", id);
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // PAQUETES POR MATERIAL
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> PorMaterial(int idMaterial)
        {
            try
            {
                var material = await _materialService.GetMaterialByIdAsync(idMaterial);
                if (material == null)
                {
                    TempData["MensajeError"] = "Material no encontrado";
                    return RedirectToAction("Index");
                }

                var paquetes = await _paqueteService.GetPaquetesByMaterialAsync(idMaterial);

                ViewBag.Material = material;
                ViewBag.TotalPaquetes = paquetes.Count();
                ViewBag.Sellados = paquetes.Count(p => p.Estado == "Sellado");
                ViewBag.Abiertos = paquetes.Count(p => p.Estado == "Abierto");

                return View(paquetes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar paquetes del material {IdMaterial}", idMaterial);
                TempData["MensajeError"] = "Error al cargar los paquetes";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // DESGLOSE POR MATERIAL
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> Desglose()
        {
            try
            {
                var desglose = await _paqueteService.GetDesglosePorMaterialAsync();
                var inconsistencias = await _paqueteService.GetInconsistenciasAsync();

                ViewBag.Inconsistencias = inconsistencias;
                ViewBag.TotalInconsistencias = inconsistencias.Count();

                return View(desglose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar desglose de paquetes");
                TempData["MensajeError"] = "Error al cargar el desglose";
                return View(new List<PaqueteDesgloseMaterialDto>());
            }
        }

        // ================================================================
        // INCONSISTENCIAS
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> Inconsistencias()
        {
            try
            {
                var inconsistencias = await _paqueteService.GetInconsistenciasAsync();

                ViewBag.Total = inconsistencias.Count();

                return View(inconsistencias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar inconsistencias");
                TempData["MensajeError"] = "Error al cargar las inconsistencias";
                return View(new List<PaqueteInconsistenciaDto>());
            }
        }

        // ================================================================
        // CONSUMIR PAQUETES - GET
        // ================================================================

        [PermisoRequerido("PAQUETE.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Consumir(int idMaterial)
        {
            try
            {
                var material = await _materialService.GetMaterialByIdAsync(idMaterial);
                if (material == null)
                {
                    TempData["MensajeError"] = "Material no encontrado";
                    return RedirectToAction("Index");
                }

                var desglose = await _paqueteService.GetDesglosePorMaterialAsync(idMaterial);
                ViewBag.Desglose = desglose;

                return View(new PaqueteConsumirDto
                {
                    IdMaterial = idMaterial
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de consumo");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // CONSUMIR PAQUETES - POST
        // ================================================================

        [PermisoRequerido("PAQUETE.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Consumir(PaqueteConsumirDto model)
        {
            if (!ModelState.IsValid)
            {
                var desglose = await _paqueteService.GetDesglosePorMaterialAsync(model.IdMaterial);
                ViewBag.Desglose = desglose;
                return View(model);
            }

            try
            {
                var result = await _paqueteService.ConsumirAsync(
                    model.IdMaterial,
                    model.CantidadAConsumir,
                    model.Observaciones,
                    model.IdProyecto);

                if (result.Exito)
                {
                    TempData["MensajeExito"] = result.Mensaje;
                    return RedirectToAction("PorMaterial", new { idMaterial = model.IdMaterial });
                }

                TempData["MensajeError"] = result.Mensaje;
                var desglose = await _paqueteService.GetDesglosePorMaterialAsync(model.IdMaterial);
                ViewBag.Desglose = desglose;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consumir paquetes");
                TempData["MensajeError"] = "Error al consumir paquetes";
                return RedirectToAction("PorMaterial", new { idMaterial = model.IdMaterial });
            }
        }

        // ================================================================
        // REPONER PAQUETES - GET
        // ================================================================

        [PermisoRequerido("PAQUETE.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Reponer(int idMaterial)
        {
            try
            {
                var material = await _materialService.GetMaterialByIdAsync(idMaterial);
                if (material == null)
                {
                    TempData["MensajeError"] = "Material no encontrado";
                    return RedirectToAction("Index");
                }

                var desglose = await _paqueteService.GetDesglosePorMaterialAsync(idMaterial);
                ViewBag.Desglose = desglose;

                return View(new PaqueteReponerDto
                {
                    IdMaterial = idMaterial
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de reposición");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // REPONER PAQUETES - POST
        // ================================================================

        [PermisoRequerido("PAQUETE.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reponer(PaqueteReponerDto model)
        {
            if (!ModelState.IsValid)
            {
                var desglose = await _paqueteService.GetDesglosePorMaterialAsync(model.IdMaterial);
                ViewBag.Desglose = desglose;
                return View(model);
            }

            try
            {
                var result = await _paqueteService.ReponerAsync(
                    model.IdMaterial,
                    model.CantidadAReponer,
                    model.Observaciones);

                if (result.Exito)
                {
                    TempData["MensajeExito"] = result.Mensaje;
                    return RedirectToAction("PorMaterial", new { idMaterial = model.IdMaterial });
                }

                TempData["MensajeError"] = result.Mensaje;
                var desglose = await _paqueteService.GetDesglosePorMaterialAsync(model.IdMaterial);
                ViewBag.Desglose = desglose;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reponer paquetes");
                TempData["MensajeError"] = "Error al reponer paquetes";
                return RedirectToAction("PorMaterial", new { idMaterial = model.IdMaterial });
            }
        }

        // ================================================================
        // RESUMEN DE PAQUETES (AJAX)
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetResumen()
        {
            try
            {
                var resumen = await _paqueteService.GetResumenAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        resumen.TotalPaquetes,
                        resumen.PaquetesSellados,
                        resumen.PaquetesAbiertos,
                        resumen.PaquetesAgotados,
                        resumen.TotalUnidadesEnPaquetes,
                        resumen.TotalCapacidad,
                        resumen.PorcentajeOcupado,
                        resumen.MaterialesConPaquetes,
                        resumen.MaterialesSinPaquetes,
                        resumen.PaquetesInconsistentes,
                        resumen.UltimoPaqueteCreado,
                        resumen.UltimaApertura,
                        resumen.PaquetesPorEstado
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de paquetes");
                return Json(new { success = false, message = "Error al obtener resumen" });
            }
        }

        // ================================================================
        // OBTENER PAQUETES POR MATERIAL (AJAX)
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetPaquetesByMaterial(int idMaterial)
        {
            try
            {
                var paquetes = await _paqueteService.GetPaquetesByMaterialAsync(idMaterial);

                return Json(new
                {
                    success = true,
                    data = paquetes.Select(p => new
                    {
                        p.IdPaquete,
                        p.NombrePresentacion,
                        p.Estado,
                        p.CapacidadUnidadBase,
                        p.CantidadActualUnidadBase,
                        p.PorcentajeDisponible,
                        p.FechaIngresoFormateada,
                        p.FechaAperturaFormateada,
                        p.EstadoBadge,
                        p.DisplayText
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener paquetes por material");
                return Json(new { success = false, message = "Error al obtener paquetes" });
            }
        }

        // ================================================================
        // EXPORTAR DESGLOSE A EXCEL
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> ExportarDesgloseExcel()
        {
            try
            {
                var desglose = await _paqueteService.GetDesglosePorMaterialAsync();

                if (!desglose.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Desglose");
                }

                var data = desglose.Select(d => new
                {
                    d.CodigoBarras,
                    d.Material,
                    d.Presentacion,
                    d.PaquetesSellados,
                    d.UnidadesEnSellados,
                    d.PaquetesAbiertos,
                    d.UnidadesEnAbiertos,
                    d.TotalUnidadesEnPaquetes,
                    d.StockActualMaterial,
                    Consistencia = d.EstaConsistente ? "OK" : "Inconsistente"
                });

                var columnas = new[] { "Código", "Material", "Presentación", "Paquetes Sellados", "Unidades en Sellados",
                                       "Paquetes Abiertos", "Unidades en Abiertos", "Total en Paquetes", "Stock Actual", "Consistencia" };

                var bytes = await _reporteService.ExportarExcelAsync(data, "Desglose_Paquetes", columnas);

                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Desglose_Paquetes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar desglose a Excel");
                TempData["MensajeError"] = "Error al exportar";
                return RedirectToAction("Desglose");
            }
        }

        // ================================================================
        // OBTENER PAQUETE POR ID (AJAX)
        // ================================================================

        [PermisoRequerido("PAQUETE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetPaqueteById(long id)
        {
            try
            {
                var paquete = await _paqueteService.GetPaqueteByIdAsync(id);

                if (paquete == null)
                {
                    return Json(new { success = false, message = "Paquete no encontrado" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        paquete.IdPaquete,
                        paquete.Material,
                        paquete.CodigoBarras,
                        paquete.NombrePresentacion,
                        paquete.CapacidadUnidadBase,
                        paquete.CantidadActualUnidadBase,
                        paquete.Estado,
                        paquete.FechaIngresoFormateada,
                        paquete.FechaAperturaFormateada,
                        paquete.FechaAgotamientoFormateada,
                        paquete.PorcentajeDisponible,
                        paquete.EstaSellado,
                        paquete.EstaAbierto,
                        paquete.EstaAgotado
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener paquete por ID");
                return Json(new { success = false, message = "Error al obtener paquete" });
            }
        }
    }
}
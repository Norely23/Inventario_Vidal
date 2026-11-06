using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    
    public class ReportesController : Controller
    {
        private readonly IReporteService _reporteService;
        private readonly IReportePdfService _reportePdfService;
        private readonly IProyectoService _proyectoService;
        private readonly IMaterialService _materialService;
        private readonly IProveedorService _proveedorService;
        private readonly ILogger<ReportesController> _logger;
        private readonly FabricacionesVidalEirlBdContext _context;

        public ReportesController(
            IReporteService reporteService,
            IReportePdfService reportePdfService,
            IProyectoService proyectoService,
            IMaterialService materialService,
            IProveedorService proveedorService,
            ILogger<ReportesController> logger,
            FabricacionesVidalEirlBdContext context)
        {
            _reporteService = reporteService;
            _reportePdfService = reportePdfService;
            _proyectoService = proyectoService;
            _materialService = materialService;
            _proveedorService = proveedorService;
            _logger = logger;
            _context = context;
        }

        // ================================================================
        // INDEX - PÁGINA PRINCIPAL DE REPORTES
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // ================================================================
        // REPORTE - STOCK
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Stock(bool soloAlertas = false, string search = "", int? idCategoria = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteStockAsync(soloAlertas);

                var query = reporte.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(r =>
                        r.Nombre.ToLower().Contains(search) ||
                        r.CodigoBarras.ToLower().Contains(search));
                }

                if (idCategoria.HasValue)
                {
                    var materialesFiltrados = await _context.Materiales
                        .Where(m => m.IdCategoria == idCategoria.Value && m.Activo)
                        .Select(m => m.IdMaterial)
                        .ToListAsync();

                    query = query.Where(r => materialesFiltrados.Contains(r.IdMaterial));
                }

                var result = query.ToList();

                var categorias = await _context.Categorias
                    .Where(c => c.Activo)
                    .Select(c => new { c.IdCategoria, c.Nombre })
                    .OrderBy(c => c.Nombre)
                    .ToListAsync();

                ViewBag.SoloAlertas = soloAlertas;
                ViewBag.TotalRegistros = result.Count;
                ViewBag.Alertas = result.Count(r => r.AlertaStockBajo);
                ViewBag.Categorias = categorias;
                ViewBag.Search = search;
                ViewBag.IdCategoria = idCategoria;

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de stock");
                TempData["MensajeError"] = "Error al generar el reporte de stock";
                return View(new List<ReporteStockDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarStockExcel(bool soloAlertas = false)
        {
            try
            {
                var reporte = await _reporteService.GetReporteStockAsync(soloAlertas);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Stock");
                }

                var data = reporte.Select(r => new
                {
                    r.CodigoBarras,
                    r.Nombre,
                    r.Categoria,
                    r.UnidadConsumo,
                    r.StockActual,
                    r.StockMinimo,
                    r.Valorizado,
                    r.Estado
                });

                var columnasExcel = new[] { "Código", "Material", "Categoría", "Unidad", "Stock Actual", "Stock Mínimo", "Valorizado", "Estado" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Stock", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Stock_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de stock a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Stock");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarStockPdf(bool soloAlertas = false)
        {
            try
            {
                var reporte = await _reporteService.GetReporteStockAsync(soloAlertas);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Stock");
                }

                var bytes = await _reportePdfService.GenerarPdfStockAsync(reporte, soloAlertas);
                return File(bytes, "application/pdf", $"Reporte_Stock_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de stock a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Stock");
            }
        }

        // ================================================================
        // REPORTE - PROYECTOS
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Proyectos(DateTime? desde = null, DateTime? hasta = null, string? estado = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteProyectosAsync(desde, hasta);

                if (!string.IsNullOrEmpty(estado))
                {
                    reporte = reporte.Where(r => r.Estado == estado);
                }

                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                ViewBag.Estado = estado;
                ViewBag.TotalRegistros = reporte.Count();
                ViewBag.TotalUtilidad = reporte.Sum(r => r.UtilidadReal ?? 0);

                return View(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de proyectos");
                TempData["MensajeError"] = "Error al generar el reporte de proyectos";
                return View(new List<ReporteProyectoDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarProyectosExcel(DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteProyectosAsync(desde, hasta);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Proyectos");
                }

                var data = reporte.Select(r => new
                {
                    r.Codigo,
                    r.Cliente,
                    r.Estado,
                    r.FechaInicio,
                    r.FechaEntregaReal,
                    r.PrecioVenta,
                    r.CostoEstimado,
                    r.CostoReal,
                    r.UtilidadReal,
                    r.Moneda
                });

                var columnasExcel = new[] { "Código", "Cliente", "Estado", "Fecha Inicio", "Fecha Entrega", "Precio Venta", "Costo Estimado", "Costo Real", "Utilidad", "Moneda" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Proyectos", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Proyectos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de proyectos a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Proyectos");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarProyectosPdf(DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteProyectosAsync(desde, hasta);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Proyectos");
                }

                var bytes = await _reportePdfService.GenerarPdfProyectosAsync(reporte, desde, hasta);
                return File(bytes, "application/pdf", $"Reporte_Proyectos_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de proyectos a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Proyectos");
            }
        }

        // ================================================================
        // REPORTE - COMPRAS
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Compras(DateTime? desde = null, DateTime? hasta = null, int? idProveedor = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteComprasAsync(desde, hasta, idProveedor);
                var proveedores = await _proveedorService.GetProveedoresActivosAsync();

                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                ViewBag.IdProveedor = idProveedor;
                ViewBag.Proveedores = proveedores;
                ViewBag.TotalRegistros = reporte.Count();
                ViewBag.TotalGastado = reporte.Sum(r => r.Subtotal ?? 0);

                return View(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de compras");
                TempData["MensajeError"] = "Error al generar el reporte de compras";
                return View(new List<ReporteCompraDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarComprasExcel(DateTime? desde = null, DateTime? hasta = null, int? idProveedor = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteComprasAsync(desde, hasta, idProveedor);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Compras");
                }

                var data = reporte.Select(r => new
                {
                    r.NumeroComprobante,
                    r.Proveedor,
                    r.FechaCompra,
                    r.Material,
                    r.Cantidad,
                    r.CostoUnitario,
                    r.Subtotal,
                    r.Moneda,
                    r.Estado
                });

                var columnasExcel = new[] { "Comprobante", "Proveedor", "Fecha", "Material", "Cantidad", "Costo Unitario", "Subtotal", "Moneda", "Estado" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Compras", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Compras_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de compras a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Compras");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarComprasPdf(DateTime? desde = null, DateTime? hasta = null, int? idProveedor = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteComprasAsync(desde, hasta, idProveedor);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Compras");
                }

                var bytes = await _reportePdfService.GenerarPdfComprasAsync(reporte, desde, hasta, idProveedor);
                return File(bytes, "application/pdf", $"Reporte_Compras_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de compras a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Compras");
            }
        }

        // ================================================================
        // REPORTE - KARDEX
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Kardex(int? idMaterial = null, DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteKardexAsync(idMaterial, desde, hasta);
                var materiales = await _materialService.GetMaterialesSelectAsync();

                ViewBag.IdMaterial = idMaterial;
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                ViewBag.Materiales = materiales;
                ViewBag.TotalRegistros = reporte.Count();

                return View(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de kardex");
                TempData["MensajeError"] = "Error al generar el reporte de kardex";
                return View(new List<ReporteKardexDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarKardexExcel(int? idMaterial = null, DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteKardexAsync(idMaterial, desde, hasta);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Kardex");
                }

                var data = reporte.Select(r => new
                {
                    r.FechaMovimiento,
                    r.Material,
                    r.TipoMovimiento,
                    r.Cantidad,
                    r.StockAnterior,
                    r.StockPosterior,
                    r.RegistradoPor,
                    r.Observacion
                });

                var columnasExcel = new[] { "Fecha", "Material", "Tipo Movimiento", "Cantidad", "Stock Anterior", "Stock Posterior", "Registrado Por", "Observación" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Kardex", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Kardex_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de kardex a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Kardex");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarKardexPdf(int? idMaterial = null, DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteKardexAsync(idMaterial, desde, hasta);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Kardex");
                }

                var bytes = await _reportePdfService.GenerarPdfKardexAsync(reporte, idMaterial, desde, hasta);
                return File(bytes, "application/pdf", $"Reporte_Kardex_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de kardex a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Kardex");
            }
        }

        // ================================================================
        // REPORTE - MAQUINARIA
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Maquinaria()
        {
            try
            {
                var reporte = await _reporteService.GetReporteMaquinariaAsync();

                ViewBag.TotalRegistros = reporte.Count();
                ViewBag.TotalPrestamos = reporte.Sum(r => r.TotalPrestamos);
                ViewBag.TotalMantenimientos = reporte.Sum(r => r.TotalMantenimientos);

                return View(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de maquinaria");
                TempData["MensajeError"] = "Error al generar el reporte de maquinaria";
                return View(new List<ReporteMaquinariaDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarMaquinariaExcel()
        {
            try
            {
                var reporte = await _reporteService.GetReporteMaquinariaAsync();

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Maquinaria");
                }

                var data = reporte.Select(r => new
                {
                    r.Codigo,
                    r.Nombre,
                    r.Marca,
                    r.Modelo,
                    r.Estado,
                    r.TotalPrestamos,
                    r.TotalMantenimientos,
                    r.CostoMantenimiento
                });

                var columnasExcel = new[] { "Código", "Nombre", "Marca", "Modelo", "Estado", "Préstamos", "Mantenimientos", "Costo Mantenimiento" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Maquinaria", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Maquinaria_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de maquinaria a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Maquinaria");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarMaquinariaPdf()
        {
            try
            {
                var reporte = await _reporteService.GetReporteMaquinariaAsync();

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Maquinaria");
                }

                var bytes = await _reportePdfService.GenerarPdfMaquinariaAsync(reporte);
                return File(bytes, "application/pdf", $"Reporte_Maquinaria_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de maquinaria a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Maquinaria");
            }
        }

        // ================================================================
        // REPORTE - CLIENTES
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Clientes(string search = "", string tipo = "", string orden = "proyectos")
        {
            try
            {
                var reporte = await _reporteService.GetReporteClientesAsync(search, tipo, orden);

                ViewBag.TotalRegistros = reporte.Count();
                ViewBag.TotalProyectos = reporte.Sum(r => r.TotalProyectos);
                ViewBag.TotalInvertido = reporte.Sum(r => r.TotalInvertido);
                ViewBag.Search = search;
                ViewBag.Tipo = tipo;
                ViewBag.Orden = orden;

                return View(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de clientes");
                TempData["MensajeError"] = "Error al generar el reporte de clientes";
                return View(new List<ReporteClienteDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarClientesExcel(string search = "", string tipo = "", string orden = "proyectos")
        {
            try
            {
                var reporte = await _reporteService.GetReporteClientesAsync(search, tipo, orden);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Clientes");
                }

                var data = reporte.Select(r => new
                {
                    r.NumeroDocumento,
                    r.NombreOrazonSocial,
                    r.TipoCliente,
                    r.Telefono,
                    r.CorreoElectronico,
                    r.TotalProyectos,
                    r.TotalInvertido
                });

                var columnasExcel = new[] { "RUC/DNI", "Nombre", "Tipo", "Teléfono", "Email", "Proyectos", "Total Invertido" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Clientes", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Clientes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de clientes a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Clientes");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarClientesPdf(string search = "", string tipo = "", string orden = "proyectos")
        {
            try
            {
                var reporte = await _reporteService.GetReporteClientesAsync(search, tipo, orden);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Clientes");
                }

                var bytes = await _reportePdfService.GenerarPdfClientesAsync(reporte);
                return File(bytes, "application/pdf", $"Reporte_Clientes_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de clientes a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Clientes");
            }
        }

        // ================================================================
        // REPORTE - CONTRATISTAS
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Contratistas(string search = "", string especialidad = "", string orden = "horas")
        {
            try
            {
                var reporte = await _reporteService.GetReporteContratistasAsync(search, especialidad, orden);

                ViewBag.TotalRegistros = reporte.Count();
                ViewBag.TotalHoras = reporte.Sum(r => r.TotalHoras);
                ViewBag.TotalPagado = reporte.Sum(r => r.TotalPagado);
                ViewBag.Search = search;
                ViewBag.Especialidad = especialidad;
                ViewBag.Orden = orden;

                return View(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de contratistas");
                TempData["MensajeError"] = "Error al generar el reporte de contratistas";
                return View(new List<ReporteContratistaDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarContratistasExcel(string search = "", string especialidad = "", string orden = "horas")
        {
            try
            {
                var reporte = await _reporteService.GetReporteContratistasAsync(search, especialidad, orden);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Contratistas");
                }

                var data = reporte.Select(r => new
                {
                    r.NumeroDocumento,
                    r.NombreCompleto,
                    r.Especialidad,
                    r.Telefono,
                    r.CorreoElectronico,
                    r.TotalHoras,
                    r.TarifaHora,
                    r.TotalPagado
                });

                var columnasExcel = new[] { "DNI", "Nombre", "Especialidad", "Teléfono", "Email", "Horas", "Tarifa/Hora", "Total Pagado" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Contratistas", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Contratistas_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de contratistas a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Contratistas");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarContratistasPdf(string search = "", string especialidad = "", string orden = "horas")
        {
            try
            {
                var reporte = await _reporteService.GetReporteContratistasAsync(search, especialidad, orden);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Contratistas");
                }

                var bytes = await _reportePdfService.GenerarPdfContratistasAsync(reporte);
                return File(bytes, "application/pdf", $"Reporte_Contratistas_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de contratistas a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Contratistas");
            }
        }

        // ================================================================
        // REPORTE - UTILIDAD
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Utilidad(string search = "", string resultado = "", DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteUtilidadAsync(search, resultado, desde, hasta);

                ViewBag.TotalRegistros = reporte.Count();
                ViewBag.TotalIngresos = reporte.Sum(r => r.Ingresos);
                ViewBag.TotalCostos = reporte.Sum(r => r.Costos);
                ViewBag.TotalUtilidad = reporte.Sum(r => r.Utilidad);
                ViewBag.MargenPromedio = reporte.Any() ? reporte.Average(r => r.Margen) : 0;
                ViewBag.ProyectosGanancia = reporte.Count(r => r.Utilidad >= 0);
                ViewBag.ProyectosPerdida = reporte.Count(r => r.Utilidad < 0);
                ViewBag.Search = search;
                ViewBag.Resultado = resultado;
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;

                return View(reporte);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de utilidad");
                TempData["MensajeError"] = "Error al generar el reporte de utilidad";
                return View(new List<ReporteUtilidadDto>());
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarUtilidadExcel(string search = "", string resultado = "", DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteUtilidadAsync(search, resultado, desde, hasta);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Utilidad");
                }

                var data = reporte.Select(r => new
                {
                    r.Codigo,
                    r.Nombre,
                    r.Cliente,
                    r.FechaInicio,
                    r.Ingresos,
                    r.Costos,
                    r.Utilidad,
                    r.Margen,
                    r.Moneda,
                    r.Estado
                });

                var columnasExcel = new[] { "Código", "Proyecto", "Cliente", "Fecha", "Ingresos", "Costos", "Utilidad", "Margen", "Moneda", "Estado" };
                var bytes = await _reporteService.ExportarExcelAsync(data, "Reporte_Utilidad", columnasExcel);
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Utilidad_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de utilidad a Excel");
                TempData["MensajeError"] = "Error al exportar el reporte";
                return RedirectToAction("Utilidad");
            }
        }

        [PermisoRequerido("REPORTE.EXPORTAR")]
        [HttpGet]
        public async Task<IActionResult> ExportarUtilidadPdf(string search = "", string resultado = "", DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var reporte = await _reporteService.GetReporteUtilidadAsync(search, resultado, desde, hasta);

                if (!reporte.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Utilidad");
                }

                var bytes = await _reportePdfService.GenerarPdfUtilidadAsync(reporte);
                return File(bytes, "application/pdf", $"Reporte_Utilidad_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar reporte de utilidad a PDF");
                TempData["MensajeError"] = "Error al exportar el reporte PDF";
                return RedirectToAction("Utilidad");
            }
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetEstadisticasRapidas()
        {
            try
            {
                var estadisticas = await _reporteService.GetEstadisticasRapidasAsync();
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        estadisticas.TotalMateriales,
                        estadisticas.TotalProyectos,
                        estadisticas.TotalCompras,
                        estadisticas.AlertasStock,
                        estadisticas.TotalClientes,
                        estadisticas.TotalContratistas,
                        estadisticas.TotalMaquinarias,
                        estadisticas.UtilidadTotal,
                        estadisticas.TotalComprasValor,
                        estadisticas.ProyectosActivos,
                        estadisticas.ProyectosEntregados
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas rápidas");
                return Json(new { success = false, message = "Error al obtener estadísticas" });
            }
        }

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetMaterialesMasUtilizados(int cantidad = 10, DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var materiales = await _reporteService.GetMaterialesMasUtilizadosAsync(cantidad, desde, hasta);

                return Json(new
                {
                    success = true,
                    data = materiales.Select(m => new
                    {
                        m.IdMaterial,
                        m.Material,
                        m.Cantidad,
                        m.Porcentaje
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materiales más utilizados");
                return Json(new { success = false, message = "Error al obtener datos" });
            }
        }

        [PermisoRequerido("REPORTE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetConsumoProyecto(int idProyecto)
        {
            try
            {
                var consumo = await _reporteService.GetReporteConsumoProyectoAsync(idProyecto);

                return Json(new
                {
                    success = true,
                    data = consumo.Select(c => new
                    {
                        c.IdMaterial,
                        c.Material,
                        c.CodigoBarras,
                        c.CategoriaMaterial,
                        c.UnidadConsumo,
                        c.CantidadAsignada,
                        c.CantidadUtilizada,
                        c.CantidadDevuelta,
                        c.TotalCosto,
                        c.PorcentajeUtilizado
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener consumo del proyecto");
                return Json(new { success = false, message = "Error al obtener datos" });
            }
        }
    }
}
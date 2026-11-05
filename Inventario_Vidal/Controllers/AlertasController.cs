using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    [PermisoRequerido("ALERTA.VER")]
    public class AlertasController : Controller
    {
        private readonly IAlertaService _alertaService;
        private readonly ILogger<AlertasController> _logger;

        public AlertasController(IAlertaService alertaService, ILogger<AlertasController> logger)
        {
            _alertaService = alertaService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - RESUMEN DE ALERTAS
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var resumen = await _alertaService.GetResumenAlertasAsync();
                ViewBag.Resumen = resumen;

                var stockBajo = await _alertaService.GetAlertasStockBajoAsync();
                var devolucionesContratistas = await _alertaService.GetAlertasDevolucionesContratistasAsync();
                var devolucionesEmpleados = await _alertaService.GetAlertasDevolucionesEmpleadosAsync();
                var prestamosVencidos = await _alertaService.GetAlertasPrestamosVencidosAsync();

                ViewBag.StockBajo = stockBajo;
                ViewBag.DevolucionesContratistas = devolucionesContratistas;
                ViewBag.DevolucionesEmpleados = devolucionesEmpleados;
                ViewBag.PrestamosVencidos = prestamosVencidos;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar resumen de alertas");
                TempData["MensajeError"] = "Error al cargar las alertas";
                return View();
            }
        }

        // ================================================================
        // STOCK BAJO - LISTADO
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> StockBajo()
        {
            try
            {
                var alertas = await _alertaService.GetAlertasStockBajoAsync();
                ViewBag.Total = alertas.Count();
                return View(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar alertas de stock bajo");
                TempData["MensajeError"] = "Error al cargar las alertas de stock bajo";
                return View(new List<AlertasStockDto>());
            }
        }

        // ================================================================
        // DEVOLUCIONES CONTRATISTAS - LISTADO
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> DevolucionesContratistas()
        {
            try
            {
                var alertas = await _alertaService.GetAlertasDevolucionesContratistasAsync();
                ViewBag.Total = alertas.Count();
                return View(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar alertas de devoluciones de contratistas");
                TempData["MensajeError"] = "Error al cargar las alertas";
                return View(new List<ContratistaRecursoAsignadoDto>());
            }
        }

        // ================================================================
        // DEVOLUCIONES EMPLEADOS - LISTADO
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> DevolucionesEmpleados()
        {
            try
            {
                var alertas = await _alertaService.GetAlertasDevolucionesEmpleadosAsync();
                ViewBag.Total = alertas.Count();
                return View(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar alertas de devoluciones de empleados");
                TempData["MensajeError"] = "Error al cargar las alertas";
                return View(new List<EmpleadoRecursoAsignadoDto>());
            }
        }

        // ================================================================
        // PRÉSTAMOS VENCIDOS - LISTADO
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> PrestamosVencidos()
        {
            try
            {
                var alertas = await _alertaService.GetAlertasPrestamosVencidosAsync();
                ViewBag.Total = alertas.Count();
                return View(alertas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar alertas de préstamos vencidos");
                TempData["MensajeError"] = "Error al cargar las alertas";
                return View(new List<PrestamoMaquinariaListDto>());
            }
        }

        // ================================================================
        // MARCAR TODAS COMO LEÍDAS - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasLeidas()
        {
            try
            {
                var result = await _alertaService.MarcarTodasComoLeidasAsync();

                if (result)
                {
                    TempData["MensajeExito"] = "Todas las alertas han sido marcadas como leídas";
                }
                else
                {
                    TempData["MensajeError"] = "Error al marcar las alertas como leídas";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar todas las alertas como leídas");
                TempData["MensajeError"] = "Error al procesar la solicitud";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // OBTENER NOTIFICACIONES PARA LA CAMPANITA (AJAX)
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> GetNotificaciones()
        {
            try
            {
                var resumen = await _alertaService.GetResumenAlertasAsync();
                var totalNoLeidas = resumen.TotalAlertas;

                // Obtener listado de alertas
                var alertas = new List<AlertaDto>();

                // Stock bajo
                var stockBajo = await _alertaService.GetAlertasStockBajoAsync();
                foreach (var item in stockBajo.Take(3))
                {
                    alertas.Add(new AlertaDto
                    {
                        IdAlerta = item.IdMaterial,
                        Tipo = "stock_bajo",
                        Titulo = $"Stock bajo: {item.Nombre}",
                        Descripcion = $"El material {item.Nombre} tiene {item.StockActual} {item.UnidadConsumo} disponibles. Mínimo: {item.StockMinimo}",
                        Referencia = item.CodigoBarras,
                        IdReferencia = item.IdMaterial,
                        Enlace = $"/Materiales/Detalles/{item.IdMaterial}",
                        Icono = "alert-triangle",
                        Color = "danger",
                        Leida = false,
                        FechaCreacion = DateTime.Now
                    });
                }

                // Devoluciones contratistas
                var devContratistas = await _alertaService.GetAlertasDevolucionesContratistasAsync();
                foreach (var item in devContratistas.Take(3))
                {
                    alertas.Add(new AlertaDto
                    {
                        IdAlerta = item.IdAsignacionRecurso + 10000,
                        Tipo = "devolucion_contratista",
                        Titulo = $"Devolución pendiente",
                        Descripcion = $"El contratista {item.Contratista} debe devolver {item.PendienteDevolucion:N2} {item.Material}",
                        Referencia = item.Contratista,
                        IdReferencia = item.IdContratista,
                        Enlace = $"/Contratistas/Detalles/{item.IdContratista}",
                        Icono = "hardhat",
                        Color = "warning",
                        Leida = false,
                        FechaCreacion = item.FechaEntrega
                    });
                }

                // Devoluciones empleados
                var devEmpleados = await _alertaService.GetAlertasDevolucionesEmpleadosAsync();
                foreach (var item in devEmpleados.Take(3))
                {
                    alertas.Add(new AlertaDto
                    {
                        IdAlerta = item.IdAsignacionRecursoEmpleado + 20000,
                        Tipo = "devolucion_empleado",
                        Titulo = $"Devolución pendiente",
                        Descripcion = $"El empleado {item.Empleado} debe devolver {item.PendienteDevolucion:N2} {item.Material}",
                        Referencia = item.Empleado,
                        IdReferencia = item.IdEmpleado,
                        Enlace = $"/Empleados/Detalles/{item.IdEmpleado}",
                        Icono = "user",
                        Color = "warning",
                        Leida = false,
                        FechaCreacion = item.FechaEntrega
                    });
                }

                // Préstamos vencidos
                var prestamos = await _alertaService.GetAlertasPrestamosVencidosAsync();
                foreach (var item in prestamos.Take(3))
                {
                    alertas.Add(new AlertaDto
                    {
                        IdAlerta = item.IdPrestamo + 30000,
                        Tipo = "prestamo_vencido",
                        Titulo = $"Préstamo vencido",
                        Descripcion = $"La maquinaria {item.Maquinaria} prestada a {item.Destinatario}",
                        Referencia = item.Maquinaria,
                        IdReferencia = item.IdMaquinaria,
                        Enlace = $"/Maquinarias/Detalles/{item.IdMaquinaria}",
                        Icono = "tool",
                        Color = "danger",
                        Leida = false,
                        FechaCreacion = item.FechaPrestamo
                    });
                }

                return Json(new
                {
                    totalNoLeidas = totalNoLeidas,
                    notificaciones = alertas
                        .OrderByDescending(a => a.FechaCreacion)
                        .Take(10)
                        .Select(a => new
                        {
                            a.IdAlerta,
                            a.Tipo,
                            a.Titulo,
                            a.Descripcion,
                            a.Enlace,
                            a.Icono,
                            a.Color,
                            a.Leida,
                            Tiempo = a.TiempoTranscurrido,
                            Fecha = a.FechaFormateada
                        })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener notificaciones");
                return Json(new { totalNoLeidas = 0, notificaciones = new List<object>() });
            }
        }

        // ================================================================
        // MARCAR COMO LEÍDA (AJAX)
        // ================================================================

        [HttpPost]
        public async Task<IActionResult> MarcarComoLeida(int id)
        {
            try
            {
                var result = await _alertaService.MarcarComoLeidaAsync(id);

                if (result)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Error al marcar como leída" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar alerta como leída");
                return Json(new { success = false, message = "Error interno" });
            }
        }

        // ================================================================
        // MARCAR TODAS COMO LEÍDAS (AJAX)
        // ================================================================

        [HttpPost]
        public async Task<IActionResult> MarcarTodasComoLeidasAjax()
        {
            try
            {
                var result = await _alertaService.MarcarTodasComoLeidasAsync();

                if (result)
                    return Json(new { success = true });
                else
                    return Json(new { success = false, message = "Error al marcar todas como leídas" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar todas las alertas como leídas");
                return Json(new { success = false, message = "Error interno" });
            }
        }
    }
}
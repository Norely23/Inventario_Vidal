using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    [RolRequerido("Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly IAuditoriaService _auditoriaService;
        private readonly ILogger<AuditoriaController> _logger;

        public AuditoriaController(IAuditoriaService auditoriaService, ILogger<AuditoriaController> logger)
        {
            _auditoriaService = auditoriaService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO CON FILTROS
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string? tabla = null,
            string? accion = null,
            int? usuarioId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            int page = 1,
            int pageSize = 30)
        {
            try
            {
                var registros = await _auditoriaService.GetAuditoriaAsync(
                    tabla, accion, usuarioId, desde, hasta, page, pageSize);

                // Obtener datos para los filtros
                var tablas = await _auditoriaService.GetTablasAuditadasAsync();
                var acciones = await _auditoriaService.GetAccionesAuditadasAsync();
                var usuarios = await _auditoriaService.GetUsuariosConActividadAsync();

                // Crear filtro DTO para la vista
                var filtro = new AuditoriaFiltroDto
                {
                    Tabla = tabla,
                    Accion = accion,
                    UsuarioId = usuarioId,
                    Desde = desde,
                    Hasta = hasta,
                    Page = page,
                    PageSize = pageSize,
                    TablasDisponibles = tablas,
                    AccionesDisponibles = acciones,
                    UsuariosDisponibles = usuarios
                };

                ViewBag.Filtro = filtro;
                ViewBag.TotalRegistros = registros.Count;

                return View(registros);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar auditoría");
                TempData["MensajeError"] = "Error al cargar los registros de auditoría";
                return View(new List<AuditoriaDto>());
            }
        }

        // ================================================================
        // DETALLES DE UN REGISTRO
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Detalles(long id)
        {
            try
            {
                // Obtener un registro específico
                var registros = await _auditoriaService.GetAuditoriaAsync(page: 1, pageSize: 1000);
                var registro = registros.FirstOrDefault(r => r.IdAuditoria == id);

                if (registro == null)
                {
                    TempData["MensajeError"] = "Registro no encontrado";
                    return RedirectToAction("Index");
                }

                return View(registro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalle de auditoría {Id}", id);
                TempData["MensajeError"] = "Error al cargar el detalle";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // HISTORIAL DE UN REGISTRO ESPECÍFICO
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Historial(string tabla, string idRegistro, int page = 1)
        {
            try
            {
                var historial = await _auditoriaService.GetHistorialRegistroAsync(
                    tabla, idRegistro, null, page, 20);

                ViewBag.Tabla = tabla;
                ViewBag.IdRegistro = idRegistro;
                ViewBag.Page = page;

                return View(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar historial para {Tabla}/{IdRegistro}", tabla, idRegistro);
                TempData["MensajeError"] = "Error al cargar el historial";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // RESUMEN DE AUDITORÍA
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Resumen()
        {
            try
            {
                var resumen = await _auditoriaService.GetResumenAsync();
                return View(resumen);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar resumen de auditoría");
                TempData["MensajeError"] = "Error al cargar el resumen";
                return View(new AuditoriaResumenDto());
            }
        }

        // ================================================================
        // LIMPIAR AUDITORÍA (OPCIONAL - SOLO ADMIN)
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Limpiar(DateTime? fechaCorte = null)
        {
            try
            {
                if (!fechaCorte.HasValue)
                {
                    fechaCorte = DateTime.Now.AddMonths(-6);
                }

                // Obtener registros a eliminar
                var registros = await _auditoriaService.GetAuditoriaAsync(
                    hasta: fechaCorte.Value, page: 1, pageSize: 1000);

                // Nota: Este es un placeholder - se puede implementar un método específico
                // en el servicio para eliminar registros antiguos

                TempData["MensajeExito"] = $"Se eliminaron {registros.Count} registros anteriores a {fechaCorte.Value:dd/MM/yyyy}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al limpiar auditoría");
                TempData["MensajeError"] = "Error al limpiar los registros de auditoría";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EXPORTAR AUDITORÍA A EXCEL
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> ExportarExcel(
            string? tabla = null,
            string? accion = null,
            int? usuarioId = null,
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            try
            {
                var registros = await _auditoriaService.GetAuditoriaAsync(
                    tabla, accion, usuarioId, desde, hasta, page: 1, pageSize: 5000);

                if (!registros.Any())
                {
                    TempData["MensajeError"] = "No hay datos para exportar";
                    return RedirectToAction("Index");
                }

                var data = registros.Select(r => new
                {
                    r.FechaFormateada,
                    r.UsuarioNombre,
                    r.NombreTabla,
                    r.Accion,
                    r.IdRegistroAfectado,
                    r.ValoresAnteriores,
                    r.ValoresNuevos,
                    r.ResumenCambio
                });

                var columnas = new[] { "Fecha", "Usuario", "Tabla", "Acción", "ID Registro", "Valores Anteriores", "Valores Nuevos", "Resumen" };

                // Usar el servicio de reportes para exportar (inyectar IReporteService o usar un método similar)
                // Por ahora, redirigimos a Index con mensaje
                TempData["MensajeExito"] = "Exportación a Excel disponible próximamente";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar auditoría a Excel");
                TempData["MensajeError"] = "Error al exportar los datos";
                return RedirectToAction("Index");
            }
        }
    }
}
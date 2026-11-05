// ================================================================
// EMPLEADOS CONTROLLER - SOLO CRUD Y ESTADÍSTICAS
// ================================================================

using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class EmpleadosController : Controller
    {
        private readonly IEmpleadoService _empleadoService;
        private readonly ILogger<EmpleadosController> _logger;

        public EmpleadosController(
            IEmpleadoService empleadoService,
            ILogger<EmpleadosController> logger)
        {
            _empleadoService = empleadoService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO DE EMPLEADOS
        // ================================================================

        [PermisoRequerido("EMPLEADO.VER")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var empleados = await _empleadoService.GetEmpleadosAsync();
                return View(empleados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleados");
                TempData["MensajeError"] = "Error al cargar los empleados";
                return View(new List<EmpleadoListDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [PermisoRequerido("EMPLEADO.GESTIONAR")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new EmpleadoCreateDto
            {
                TipoDocumento = "DNI",
                Activo = true
            });
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [PermisoRequerido("EMPLEADO.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(EmpleadoCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _empleadoService.CreateEmpleadoAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Index");
                }

                TempData["MensajeError"] = result.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear empleado");
                TempData["MensajeError"] = "Error al crear el empleado";
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [PermisoRequerido("EMPLEADO.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var empleado = await _empleadoService.GetEmpleadoByIdAsync(id);
                if (empleado == null)
                {
                    TempData["MensajeError"] = "Empleado no encontrado";
                    return RedirectToAction("Index");
                }

                var model = new EmpleadoEditDto
                {
                    IdEmpleado = empleado.IdEmpleado,
                    NombreCompleto = empleado.NombreCompleto,
                    TipoDocumento = empleado.TipoDocumento,
                    NumeroDocumento = empleado.NumeroDocumento,
                    Especialidad = empleado.Especialidad,
                    Telefono = empleado.Telefono,
                    CorreoElectronico = empleado.CorreoElectronico,
                    Direccion = empleado.Direccion,
                    FechaIngreso = empleado.FechaIngreso,
                    Activo = empleado.Activo
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar empleado para editar");
                TempData["MensajeError"] = "Error al cargar el empleado";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [PermisoRequerido("EMPLEADO.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(EmpleadoEditDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _empleadoService.UpdateEmpleadoAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Index");
                }

                TempData["MensajeError"] = result.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar empleado");
                TempData["MensajeError"] = "Error al actualizar el empleado";
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [PermisoRequerido("EMPLEADO.VER")]
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var empleado = await _empleadoService.GetEmpleadoByIdAsync(id);
                if (empleado == null)
                {
                    TempData["MensajeError"] = "Empleado no encontrado";
                    return RedirectToAction("Index");
                }

                return View(empleado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del empleado");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ACTIVAR/DESACTIVAR - POST
        // ================================================================

        [PermisoRequerido("EMPLEADO.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _empleadoService.ToggleActivoAsync(id);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del empleado {Id}", id);
                TempData["MensajeError"] = "Error al cambiar el estado del empleado";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [PermisoRequerido("EMPLEADO.VER")]
        [HttpGet]
        public async Task<IActionResult> GetEmpleadosSelect(string? term = null)
        {
            try
            {
                var empleados = await _empleadoService.GetEmpleadosActivosAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    empleados = empleados.Where(e =>
                        e.NombreCompleto.ToLower().Contains(term) ||
                        e.NumeroDocumento.ToLower().Contains(term));
                }

                var result = empleados.Select(e => new
                {
                    e.IdEmpleado,
                    e.NombreCompleto,
                    e.NumeroDocumento,
                    e.Especialidad,
                    display = $"{e.NombreCompleto} - {e.NumeroDocumento}"
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener empleados para select");
                return Json(new { success = false, message = "Error al cargar empleados" });
            }
        }

        [PermisoRequerido("EMPLEADO.VER")]
        [HttpGet]
        public async Task<IActionResult> VerificarDocumento(string documento, int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(documento))
                {
                    return Json(new { exists = false, error = true, message = "Documento requerido" });
                }

                bool existe;
                if (excludeId.HasValue)
                {
                    existe = await _empleadoService.DocumentoExistsAsync(documento, excludeId.Value);
                }
                else
                {
                    existe = await _empleadoService.DocumentoExistsAsync(documento);
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar documento de empleado");
                return Json(new { exists = false, error = true });
            }
        }
    }
}
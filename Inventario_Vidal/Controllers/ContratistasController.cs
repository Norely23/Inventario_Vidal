// ================================================================
// CONTRATISTAS CONTROLLER - SOLO CRUD Y ESTADÍSTICAS
// ================================================================

using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class ContratistasController : Controller
    {
        private readonly IContratistaService _contratistaService;
        private readonly ILogger<ContratistasController> _logger;

        public ContratistasController(
            IContratistaService contratistaService,
            ILogger<ContratistasController> logger)
        {
            _contratistaService = contratistaService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO DE CONTRATISTAS
        // ================================================================

        [PermisoRequerido("CONTRATISTA.VER")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var contratistas = await _contratistaService.GetContratistasAsync();
                return View(contratistas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar contratistas");
                TempData["MensajeError"] = "Error al cargar los contratistas";
                return View(new List<ContratistaListDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [PermisoRequerido("CONTRATISTA.GESTIONAR")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new ContratistaCreateDto
            {
                TipoDocumento = "DNI",
                Activo = true
            });
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [PermisoRequerido("CONTRATISTA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ContratistaCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _contratistaService.CreateContratistaAsync(model);

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
                _logger.LogError(ex, "Error al crear contratista");
                TempData["MensajeError"] = "Error al crear el contratista";
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [PermisoRequerido("CONTRATISTA.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var contratista = await _contratistaService.GetContratistaByIdAsync(id);
                if (contratista == null)
                {
                    TempData["MensajeError"] = "Contratista no encontrado";
                    return RedirectToAction("Index");
                }

                var model = new ContratistaEditDto
                {
                    IdContratista = contratista.IdContratista,
                    NombreCompleto = contratista.NombreCompleto,
                    TipoDocumento = contratista.TipoDocumento,
                    NumeroDocumento = contratista.NumeroDocumento,
                    Especialidad = contratista.Especialidad,
                    Telefono = contratista.Telefono,
                    CorreoElectronico = contratista.CorreoElectronico,
                    Direccion = contratista.Direccion,
                    Calificacion = contratista.Calificacion,
                    Activo = contratista.Activo
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar contratista para editar");
                TempData["MensajeError"] = "Error al cargar el contratista";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [PermisoRequerido("CONTRATISTA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ContratistaEditDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _contratistaService.UpdateContratistaAsync(model);

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
                _logger.LogError(ex, "Error al actualizar contratista");
                TempData["MensajeError"] = "Error al actualizar el contratista";
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [PermisoRequerido("CONTRATISTA.VER")]
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var contratista = await _contratistaService.GetContratistaByIdAsync(id);
                if (contratista == null)
                {
                    TempData["MensajeError"] = "Contratista no encontrado";
                    return RedirectToAction("Index");
                }

                return View(contratista);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del contratista");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ACTIVAR/DESACTIVAR - POST
        // ================================================================

        [PermisoRequerido("CONTRATISTA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _contratistaService.ToggleActivoAsync(id);

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
                _logger.LogError(ex, "Error al cambiar estado del contratista {Id}", id);
                TempData["MensajeError"] = "Error al cambiar el estado del contratista";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [PermisoRequerido("CONTRATISTA.VER")]
        [HttpGet]
        public async Task<IActionResult> GetContratistasSelect(string? term = null)
        {
            try
            {
                var contratistas = await _contratistaService.GetContratistasActivosAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    contratistas = contratistas.Where(c =>
                        c.NombreCompleto.ToLower().Contains(term) ||
                        c.NumeroDocumento.ToLower().Contains(term));
                }

                var result = contratistas.Select(c => new
                {
                    c.IdContratista,
                    c.NombreCompleto,
                    c.NumeroDocumento,
                    c.Especialidad,
                    display = $"{c.NombreCompleto} - {c.NumeroDocumento}"
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener contratistas para select");
                return Json(new { success = false, message = "Error al cargar contratistas" });
            }
        }

        [PermisoRequerido("CONTRATISTA.VER")]
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
                    existe = await _contratistaService.DocumentoExistsAsync(documento, excludeId.Value);
                }
                else
                {
                    existe = await _contratistaService.DocumentoExistsAsync(documento);
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar documento de contratista");
                return Json(new { exists = false, error = true });
            }
        }
    }
}
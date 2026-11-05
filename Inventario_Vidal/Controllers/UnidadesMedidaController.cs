using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class UnidadesMedidaController : Controller
    {
        private readonly IUnidadMedidaService _unidadMedidaService;
        private readonly ILogger<UnidadesMedidaController> _logger;

        public UnidadesMedidaController(IUnidadMedidaService unidadMedidaService, ILogger<UnidadesMedidaController> logger)
        {
            _unidadMedidaService = unidadMedidaService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO
        // ================================================================

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var unidades = await _unidadMedidaService.GetUnidadesAsync();
                return View(unidades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar unidades de medida");
                TempData["MensajeError"] = "Error al cargar las unidades de medida";
                return View(new List<UnidadMedidaDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [PermisoRequerido("UNIDAD.GESTIONAR")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new UnidadMedidaCreateDto
            {
                EsFraccionable = false
            });
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [PermisoRequerido("UNIDAD.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(UnidadMedidaCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _unidadMedidaService.CreateUnidadAsync(model);

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
                _logger.LogError(ex, "Error al crear unidad de medida");
                TempData["MensajeError"] = "Error al crear la unidad de medida";
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [PermisoRequerido("UNIDAD.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var unidad = await _unidadMedidaService.GetUnidadByIdAsync(id);
                if (unidad == null)
                {
                    TempData["MensajeError"] = "Unidad de medida no encontrada";
                    return RedirectToAction("Index");
                }

                var model = new UnidadMedidaDto
                {
                    IdUnidad = unidad.IdUnidad,
                    Nombre = unidad.Nombre,
                    Abreviatura = unidad.Abreviatura,
                    EsFraccionable = unidad.EsFraccionable,
                    CantidadMateriales = unidad.CantidadMateriales
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar unidad de medida para editar");
                TempData["MensajeError"] = "Error al cargar la unidad de medida";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [PermisoRequerido("UNIDAD.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(UnidadMedidaDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _unidadMedidaService.UpdateUnidadAsync(model);

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
                _logger.LogError(ex, "Error al actualizar unidad de medida");
                TempData["MensajeError"] = "Error al actualizar la unidad de medida";
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var unidad = await _unidadMedidaService.GetUnidadByIdAsync(id);
                if (unidad == null)
                {
                    TempData["MensajeError"] = "Unidad de medida no encontrada";
                    return RedirectToAction("Index");
                }

                return View(unidad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles de la unidad de medida");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> GetUnidadesSelect(string? term = null)
        {
            try
            {
                var unidades = await _unidadMedidaService.GetUnidadesSelectAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    unidades = unidades.Where(u =>
                        u.Nombre.ToLower().Contains(term) ||
                        u.Abreviatura.ToLower().Contains(term));
                }

                var result = unidades.Select(u => new
                {
                    u.IdUnidad,
                    u.Nombre,
                    u.Abreviatura,
                    display = $"{u.Nombre} ({u.Abreviatura})"
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener unidades de medida para select");
                return Json(new { success = false, message = "Error al cargar unidades" });
            }
        }

        // ================================================================
        // VERIFICAR SI NOMBRE EXISTE (AJAX) - CORREGIDO
        // ================================================================

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> VerificarNombre(string nombre, int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre))
                {
                    return Json(new { exists = false, error = true, message = "Nombre requerido" });
                }

                bool existe = await _unidadMedidaService.NombreExistsAsync(nombre);

                if (excludeId.HasValue && existe)
                {
                    var unidadActual = await _unidadMedidaService.GetUnidadByIdAsync(excludeId.Value);
                    if (unidadActual != null && unidadActual.Nombre == nombre)
                    {
                        existe = false;
                    }
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar nombre de unidad de medida");
                return Json(new { exists = false, error = true });
            }
        }

        // ================================================================
        // VERIFICAR SI ABREVIATURA EXISTE (AJAX) - CORREGIDO
        // ================================================================

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> VerificarAbreviatura(string abreviatura, int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(abreviatura))
                {
                    return Json(new { exists = false, error = true, message = "Abreviatura requerida" });
                }

                bool existe = await _unidadMedidaService.AbreviaturaExistsAsync(abreviatura);

                if (excludeId.HasValue && existe)
                {
                    var unidadActual = await _unidadMedidaService.GetUnidadByIdAsync(excludeId.Value);
                    if (unidadActual != null && unidadActual.Abreviatura == abreviatura)
                    {
                        existe = false;
                    }
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar abreviatura de unidad de medida");
                return Json(new { exists = false, error = true });
            }
        }

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> GetUnidadById(int id)
        {
            try
            {
                var unidad = await _unidadMedidaService.GetUnidadByIdAsync(id);

                if (unidad == null)
                {
                    return Json(new { success = false, message = "Unidad no encontrada" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        unidad.IdUnidad,
                        unidad.Nombre,
                        unidad.Abreviatura,
                        unidad.EsFraccionable
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener unidad de medida por ID");
                return Json(new { success = false, message = "Error al obtener unidad" });
            }
        }

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> GetUnidadesCompra()
        {
            try
            {
                var unidades = await _unidadMedidaService.GetUnidadesSelectAsync();
                var result = unidades.Select(u => new
                {
                    u.IdUnidad,
                    u.Nombre,
                    u.Abreviatura,
                    display = $"{u.Nombre} ({u.Abreviatura})"
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener unidades de compra");
                return Json(new { success = false, message = "Error al cargar unidades" });
            }
        }

        [PermisoRequerido("UNIDAD.VER")]
        [HttpGet]
        public async Task<IActionResult> GetUnidadesConsumo()
        {
            try
            {
                var unidades = await _unidadMedidaService.GetUnidadesSelectAsync();
                var result = unidades.Select(u => new
                {
                    u.IdUnidad,
                    u.Nombre,
                    u.Abreviatura,
                    display = $"{u.Nombre} ({u.Abreviatura})"
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener unidades de consumo");
                return Json(new { success = false, message = "Error al cargar unidades" });
            }
        }
    }
}
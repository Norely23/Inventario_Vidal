using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class MarcasController : Controller
    {
        private readonly IMarcaService _marcaService;
        private readonly ILogger<MarcasController> _logger;

        public MarcasController(IMarcaService marcaService, ILogger<MarcasController> logger)
        {
            _marcaService = marcaService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO
        // ================================================================

        [PermisoRequerido("MARCA.VER")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var marcas = await _marcaService.GetMarcasAsync();
                return View(marcas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar marcas");
                TempData["MensajeError"] = "Error al cargar las marcas";
                return View(new List<MarcaDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [PermisoRequerido("MARCA.GESTIONAR")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new MarcaCreateDto
            {
                Activo = true
            });
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [PermisoRequerido("MARCA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(MarcaCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _marcaService.CreateMarcaAsync(model);

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
                _logger.LogError(ex, "Error al crear marca");
                TempData["MensajeError"] = "Error al crear la marca";
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [PermisoRequerido("MARCA.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var marca = await _marcaService.GetMarcaByIdAsync(id);
                if (marca == null)
                {
                    TempData["MensajeError"] = "Marca no encontrada";
                    return RedirectToAction("Index");
                }

                var model = new MarcaDto
                {
                    IdMarca = marca.IdMarca,
                    Nombre = marca.Nombre,
                    Activo = marca.Activo,
                    FechaRegistro = marca.FechaRegistro,
                    CantidadMateriales = marca.CantidadMateriales
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar marca para editar");
                TempData["MensajeError"] = "Error al cargar la marca";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [PermisoRequerido("MARCA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(MarcaDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _marcaService.UpdateMarcaAsync(model);

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
                _logger.LogError(ex, "Error al actualizar marca");
                TempData["MensajeError"] = "Error al actualizar la marca";
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [PermisoRequerido("MARCA.VER")]
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var marca = await _marcaService.GetMarcaByIdAsync(id);
                if (marca == null)
                {
                    TempData["MensajeError"] = "Marca no encontrada";
                    return RedirectToAction("Index");
                }

                return View(marca);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles de la marca");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ACTIVAR/DESACTIVAR - POST
        // ================================================================

        [PermisoRequerido("MARCA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _marcaService.ToggleActivoAsync(id);

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
                _logger.LogError(ex, "Error al cambiar estado de la marca {Id}", id);
                TempData["MensajeError"] = "Error al cambiar el estado de la marca";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // OBTENER MARCAS PARA SELECT (AJAX)
        // ================================================================

        [PermisoRequerido("MARCA.VER")]
        [HttpGet]
        public async Task<IActionResult> GetMarcasSelect(string? term = null)
        {
            try
            {
                var marcas = await _marcaService.GetMarcasActivasAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    marcas = marcas.Where(m => m.Nombre.ToLower().Contains(term));
                }

                var result = marcas.Select(m => new
                {
                    m.IdMarca,
                    m.Nombre,
                    display = m.Nombre
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener marcas para select");
                return Json(new { success = false, message = "Error al cargar marcas" });
            }
        }

        // ================================================================
        // VERIFICAR SI NOMBRE EXISTE (AJAX)
        // ================================================================

        [PermisoRequerido("MARCA.VER")]
        [HttpGet]
        public async Task<IActionResult> VerificarNombre(string nombre, int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre))
                {
                    return Json(new { exists = false, error = true, message = "Nombre requerido" });
                }

                bool existe = await _marcaService.NombreExistsAsync(nombre);

                if (excludeId.HasValue && existe)
                {
                    var marcaActual = await _marcaService.GetMarcaByIdAsync(excludeId.Value);
                    if (marcaActual != null && marcaActual.Nombre == nombre)
                    {
                        existe = false;
                    }
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar nombre de marca");
                return Json(new { exists = false, error = true });
            }
        }

        // ================================================================
        // OBTENER MARCA POR ID (AJAX)
        // ================================================================

        [PermisoRequerido("MARCA.VER")]
        [HttpGet]
        public async Task<IActionResult> GetMarcaById(int id)
        {
            try
            {
                var marca = await _marcaService.GetMarcaByIdAsync(id);

                if (marca == null)
                {
                    return Json(new { success = false, message = "Marca no encontrada" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        marca.IdMarca,
                        marca.Nombre,
                        marca.Activo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener marca por ID");
                return Json(new { success = false, message = "Error al obtener marca" });
            }
        }
    }
}
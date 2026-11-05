using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class CategoriasController : Controller
    {
        private readonly ICategoriaService _categoriaService;
        private readonly ILogger<CategoriasController> _logger;

        public CategoriasController(ICategoriaService categoriaService, ILogger<CategoriasController> logger)
        {
            _categoriaService = categoriaService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO
        // ================================================================

        [PermisoRequerido("CATEGORIA.VER")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categorias = await _categoriaService.GetCategoriasAsync();
                return View(categorias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar categorías");
                TempData["MensajeError"] = "Error al cargar las categorías";
                return View(new List<CategoriaDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [PermisoRequerido("CATEGORIA.GESTIONAR")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new CategoriaCreateDto
            {
                Activo = true
            });
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [PermisoRequerido("CATEGORIA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CategoriaCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _categoriaService.CreateCategoriaAsync(model);

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
                _logger.LogError(ex, "Error al crear categoría");
                TempData["MensajeError"] = "Error al crear la categoría";
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [PermisoRequerido("CATEGORIA.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var categoria = await _categoriaService.GetCategoriaByIdAsync(id);
                if (categoria == null)
                {
                    TempData["MensajeError"] = "Categoría no encontrada";
                    return RedirectToAction("Index");
                }

                var model = new CategoriaDto
                {
                    IdCategoria = categoria.IdCategoria,
                    Nombre = categoria.Nombre,
                    Descripcion = categoria.Descripcion,
                    Prefijo = categoria.Prefijo,
                    Activo = categoria.Activo,
                    CantidadMateriales = categoria.CantidadMateriales
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar categoría para editar");
                TempData["MensajeError"] = "Error al cargar la categoría";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [PermisoRequerido("CATEGORIA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(CategoriaDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _categoriaService.UpdateCategoriaAsync(model);

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
                _logger.LogError(ex, "Error al actualizar categoría");
                TempData["MensajeError"] = "Error al actualizar la categoría";
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [PermisoRequerido("CATEGORIA.VER")]
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var categoria = await _categoriaService.GetCategoriaByIdAsync(id);
                if (categoria == null)
                {
                    TempData["MensajeError"] = "Categoría no encontrada";
                    return RedirectToAction("Index");
                }

                return View(categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles de la categoría");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ACTIVAR/DESACTIVAR - POST
        // ================================================================

        [PermisoRequerido("CATEGORIA.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _categoriaService.ToggleActivoAsync(id);

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
                _logger.LogError(ex, "Error al cambiar estado de la categoría {Id}", id);
                TempData["MensajeError"] = "Error al cambiar el estado de la categoría";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // OBTENER CATEGORÍAS PARA SELECT (AJAX)
        // ================================================================

        [PermisoRequerido("CATEGORIA.VER")]
        [HttpGet]
        public async Task<IActionResult> GetCategoriasSelect()
        {
            try
            {
                var categorias = await _categoriaService.GetCategoriasSelectAsync();
                return Json(new { success = true, data = categorias });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener categorías para select");
                return Json(new { success = false, message = "Error al cargar categorías" });
            }
        }

        // ================================================================
        // VERIFICAR SI NOMBRE EXISTE (AJAX)
        // ================================================================

        [PermisoRequerido("CATEGORIA.VER")]
        [HttpGet]
        public async Task<IActionResult> VerificarNombre(string nombre, int? excludeId = null)
        {
            try
            {
                bool existe = await _categoriaService.NombreExistsAsync(nombre);

                // Si se proporciona excludeId, verificar que no sea el mismo ID
                if (excludeId.HasValue && existe)
                {
                    var categoria = await _categoriaService.GetCategoriaByIdAsync(excludeId.Value);
                    if (categoria != null && categoria.Nombre == nombre)
                    {
                        existe = false;
                    }
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar nombre de categoría");
                return Json(new { exists = false, error = true });
            }
        }
    }
}
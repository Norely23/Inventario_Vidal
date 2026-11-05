using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    [RolRequerido("Administrador")]
    public class RolesController : Controller
    {
        private readonly IRolService _rolService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRolService rolService, ILogger<RolesController> logger)
        {
            _rolService = rolService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO DE ROLES
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var roles = await _rolService.GetRolesAsync();
                return View(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar roles");
                TempData["MensajeError"] = "Error al cargar los roles";
                return View(new List<RolListDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [HttpGet]
        public IActionResult Crear()
        {
            TempData["MensajeError"] = "Los roles oficiales ya están definidos.";
            return RedirectToAction(nameof(Index));
        }


        // ================================================================
        // CREAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear(RolCreateDto model)
        {
            TempData["MensajeError"] = "No se pueden crear roles adicionales.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var rol = await _rolService.GetRolByIdAsync(id);
                if (rol == null)
                {
                    TempData["MensajeError"] = "Rol no encontrado";
                    return RedirectToAction("Index");
                }

                var model = new RolEditDto
                {
                    IdRol = rol.IdRol,
                    NombreRol = rol.NombreRol,
                    Descripcion = rol.Descripcion,
                    Activo = rol.Activo
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar rol para editar");
                TempData["MensajeError"] = "Error al cargar el rol";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(RolEditDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var result = await _rolService.UpdateRolAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar rol");
                TempData["MensajeError"] = "Error al actualizar el rol";
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var rol = await _rolService.GetRolByIdAsync(id);
                if (rol == null)
                {
                    TempData["MensajeError"] = "Rol no encontrado";
                    return RedirectToAction("Index");
                }

                return View(rol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del rol");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // PERMISOS - GET
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> Permisos(int id)
        {
            try
            {
                var rol = await _rolService.GetRolByIdAsync(id);
                if (rol == null)
                {
                    TempData["MensajeError"] = "Rol no encontrado";
                    return RedirectToAction("Index");
                }

                var permisos = await _rolService.GetPermisosByRolAsync(id);

                var model = new RolPermisosDto
                {
                    IdRol = rol.IdRol,
                    NombreRol = rol.NombreRol,
                    Permisos = permisos
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar permisos del rol");
                TempData["MensajeError"] = "Error al cargar los permisos";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // PERMISOS - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Permisos(int id, List<int> permisosSeleccionados)
        {
            try
            {
                var result = await _rolService.UpdatePermisosAsync(id, permisosSeleccionados ?? new List<int>());

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
                _logger.LogError(ex, "Error al actualizar permisos del rol {Id}", id);
                TempData["MensajeError"] = "Error al actualizar los permisos";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ACTIVAR/DESACTIVAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _rolService.ToggleActivoAsync(id);

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
                _logger.LogError(ex, "Error al cambiar estado del rol {Id}", id);
                TempData["MensajeError"] = "Error al cambiar el estado del rol";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ELIMINAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var result = await _rolService.DeleteRolAsync(id);

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
                _logger.LogError(ex, "Error al eliminar rol {Id}", id);
                TempData["MensajeError"] = "Error al eliminar el rol";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> GetRolesSelect(string? term = null)
        {
            try
            {
                var roles = await _rolService.GetRolesSelectAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    roles = roles.Where(r => r.NombreRol.ToLower().Contains(term));
                }

                var result = roles.Select(r => new
                {
                    r.IdRol,
                    r.NombreRol,
                    display = r.NombreRol
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles para select");
                return Json(new { success = false, message = "Error al cargar roles" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerificarNombre(string nombre, int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(nombre))
                {
                    return Json(new { exists = false, error = true, message = "Nombre requerido" });
                }

                bool existe;
                if (excludeId.HasValue)
                {
                    existe = await _rolService.RolExistsAsync(nombre, excludeId.Value);
                }
                else
                {
                    existe = await _rolService.RolExistsAsync(nombre);
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar nombre de rol");
                return Json(new { exists = false, error = true });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuariosByRol(int id)
        {
            try
            {
                var rol = await _rolService.GetRolByIdAsync(id);
                if (rol == null)
                {
                    return Json(new { success = false, message = "Rol no encontrado" });
                }

                // Obtener usuarios por rol (esto requiere un método en el servicio)
                // Por ahora devolvemos solo el conteo
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        rol.IdRol,
                        rol.NombreRol,
                        rol.CantidadUsuarios
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios por rol");
                return Json(new { success = false, message = "Error al obtener usuarios" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEstadisticasRoles()
        {
            try
            {
                var roles = await _rolService.GetRolesAsync();
                var total = roles.Count();
                var activos = roles.Count(r => r.Activo);
                var inactivos = total - activos;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        total,
                        activos,
                        inactivos,
                        roles = roles.Select(r => new
                        {
                            r.IdRol,
                            r.NombreRol,
                            r.Activo,
                            r.CantidadUsuarios
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de roles");
                return Json(new { success = false, message = "Error al obtener estadísticas" });
            }
        }
    }
}
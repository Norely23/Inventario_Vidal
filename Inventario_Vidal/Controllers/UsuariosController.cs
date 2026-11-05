using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    [RolRequerido("Administrador")]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(IUsuarioService usuarioService, ILogger<UsuariosController> logger)
        {
            _usuarioService = usuarioService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO DE USUARIOS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("USUARIO.VER")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var usuarios = await _usuarioService.GetUsuariosAsync();
                return View(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar usuarios");
                TempData["MensajeError"] = "Error al cargar los usuarios";
                return View(new List<UsuarioListDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("USUARIO.CREAR")]
        public async Task<IActionResult> Crear()
        {
            try
            {
                ViewBag.Roles = await _usuarioService.GetRolesAsync();
                return View(new UsuarioCreateDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de creación de usuario");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("USUARIO.CREAR")]
        public async Task<IActionResult> Crear(UsuarioCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _usuarioService.GetRolesAsync();
                return View(model);
            }

            try
            {
                var result = await _usuarioService.CreateUsuarioAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                    ViewBag.Roles = await _usuarioService.GetRolesAsync();
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                TempData["MensajeError"] = "Error al crear el usuario";
                ViewBag.Roles = await _usuarioService.GetRolesAsync();
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================
        [HttpGet]
        [PermisoRequerido("USUARIO.EDITAR")]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    TempData["MensajeError"] = "Usuario no encontrado";
                    return RedirectToAction("Index");
                }

                var model = new UsuarioEditDto
                {
                    IdUsuario = usuario.IdUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    NombreUsuario = usuario.NombreUsuario,
                    CorreoElectronico = usuario.CorreoElectronico,
                    IdRol = usuario.IdRol,
                    Activo = usuario.Activo
                };

                var roles = await _usuarioService.GetRolesAsync();

                ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    roles,
                    "IdRol",
                    "NombreRol",
                    model.IdRol
                );

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar usuario para editar");
                TempData["MensajeError"] = "Error al cargar el usuario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("USUARIO.EDITAR")]
        public async Task<IActionResult> Editar(UsuarioEditDto model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _usuarioService.GetRolesAsync();

                ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    roles,
                    "IdRol",
                    "NombreRol",
                    model.IdRol
                );

                return View(model);
            }

            try
            {
                var result = await _usuarioService.UpdateUsuarioAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["MensajeError"] = result.Message;

                    var roles = await _usuarioService.GetRolesAsync();

                    ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                        roles,
                        "IdRol",
                        "NombreRol",
                        model.IdRol
                    );

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario");
                TempData["MensajeError"] = "Error al actualizar el usuario";

                var roles = await _usuarioService.GetRolesAsync();

                ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    roles,
                    "IdRol",
                    "NombreRol",
                    model.IdRol
                );

                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("USUARIO.VER")]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);
                if (usuario == null)
                {
                    TempData["MensajeError"] = "Usuario no encontrado";
                    return RedirectToAction("Index");
                }

                return View(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del usuario");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ACTIVAR/DESACTIVAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("USUARIO.EDITAR")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _usuarioService.ToggleActivoAsync(id);

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
                _logger.LogError(ex, "Error al cambiar estado del usuario {Id}", id);
                TempData["MensajeError"] = "Error al cambiar el estado del usuario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ELIMINAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("USUARIO.EDITAR")]
        public async Task<IActionResult> Eliminar(int id)
        {
            try
            {
                var result = await _usuarioService.DeleteUsuarioAsync(id);

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
                _logger.LogError(ex, "Error al eliminar usuario {Id}", id);
                TempData["MensajeError"] = "Error al eliminar el usuario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [HttpGet]
        [PermisoRequerido("USUARIO.VER")]
        public async Task<IActionResult> GetUsuariosSelect(string? term = null)
        {
            try
            {
                var usuarios = await _usuarioService.GetUsuariosAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    usuarios = usuarios.Where(u =>
                        u.NombreCompleto.ToLower().Contains(term) ||
                        u.NombreUsuario.ToLower().Contains(term));
                }

                var result = usuarios.Select(u => new
                {
                    u.IdUsuario,
                    u.NombreCompleto,
                    u.NombreUsuario,
                    display = $"{u.NombreCompleto} ({u.NombreUsuario})"
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios para select");
                return Json(new { success = false, message = "Error al cargar usuarios" });
            }
        }

        [HttpGet]
        [PermisoRequerido("USUARIO.CREAR")]
        public async Task<IActionResult> VerificarUsuario(string nombreUsuario, int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(nombreUsuario))
                {
                    return Json(new { exists = false, error = true, message = "Nombre de usuario requerido" });
                }

                bool existe;
                if (excludeId.HasValue)
                {
                    existe = await _usuarioService.UsuarioExistsAsync(nombreUsuario, excludeId.Value);
                }
                else
                {
                    existe = await _usuarioService.UsuarioExistsAsync(nombreUsuario);
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar nombre de usuario");
                return Json(new { exists = false, error = true });
            }
        }

        [HttpGet]
        [PermisoRequerido("USUARIO.CREAR")]
        public async Task<IActionResult> VerificarCorreo(string correo, int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(correo))
                {
                    return Json(new { exists = false, error = true, message = "Correo requerido" });
                }

                // El método CorreoExistsAsync no está en la interfaz IUsuarioService
                // Lo verificamos manualmente
                var usuarios = await _usuarioService.GetUsuariosAsync();
                bool existe = usuarios.Any(u =>
                    u.CorreoElectronico != null &&
                    u.CorreoElectronico.Equals(correo, StringComparison.OrdinalIgnoreCase));

                if (excludeId.HasValue && existe)
                {
                    existe = usuarios.Any(u =>
                        u.IdUsuario != excludeId.Value &&
                        u.CorreoElectronico != null &&
                        u.CorreoElectronico.Equals(correo, StringComparison.OrdinalIgnoreCase));
                }

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar correo de usuario");
                return Json(new { exists = false, error = true });
            }
        }

        [HttpGet]
        [PermisoRequerido("USUARIO.VER")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var roles = await _usuarioService.GetRolesAsync();

                return Json(new
                {
                    success = true,
                    data = roles.Select(r => new
                    {
                        r.IdRol,
                        r.NombreRol,
                        r.Descripcion
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles");
                return Json(new { success = false, message = "Error al cargar roles" });
            }
        }

        [HttpGet]
        [PermisoRequerido("USUARIO.VER")]
        public async Task<IActionResult> GetEstadisticasUsuarios()
        {
            try
            {
                var estadisticas = await _usuarioService.GetEstadisticasAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        estadisticas.TotalUsuarios,
                        estadisticas.UsuariosActivos,
                        estadisticas.UsuariosInactivos,
                        estadisticas.UsuariosEnLinea,
                        estadisticas.UsuariosBloqueados,
                        estadisticas.NuevosUsuariosMes,
                        estadisticas.UsuariosPorRol,
                        estadisticas.UltimoUsuarioRegistrado,
                        estadisticas.UltimoAccesoGeneral
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de usuarios");
                return Json(new { success = false, message = "Error al obtener estadísticas" });
            }
        }

        [HttpGet]
        [PermisoRequerido("USUARIO.VER")]
        public async Task<IActionResult> GetUsuarioByUsername(string username)
        {
            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    return Json(new { success = false, message = "Nombre de usuario requerido" });
                }

                var usuario = await _usuarioService.GetUsuarioByUsernameAsync(username);

                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        usuario.IdUsuario,
                        usuario.NombreCompleto,
                        usuario.NombreUsuario,
                        usuario.CorreoElectronico,
                        usuario.Activo,
                        usuario.NombreRol
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por nombre de usuario");
                return Json(new { success = false, message = "Error al obtener usuario" });
            }
        }

        [HttpGet]
        [PermisoRequerido("USUARIO.VER")]
        public async Task<IActionResult> GetUsuarioById(int id)
        {
            try
            {
                var usuario = await _usuarioService.GetUsuarioByIdAsync(id);

                if (usuario == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        usuario.IdUsuario,
                        usuario.NombreCompleto,
                        usuario.NombreUsuario,
                        usuario.CorreoElectronico,
                        usuario.Activo,
                        usuario.NombreRol,
                        usuario.TotalProyectosCreados,
                        usuario.TotalMovimientosKardex,
                        usuario.TotalComprasRegistradas,
                        usuario.UltimoAcceso,
                        usuario.SesionesActivas
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por ID");
                return Json(new { success = false, message = "Error al obtener usuario" });
            }
        }
    }
}
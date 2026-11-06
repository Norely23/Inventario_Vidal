using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Helpers;
using Inventario_Vidal.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventario_Vidal.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<UsuarioService> _logger;
        private readonly IAuditoriaService _auditoriaService;

        public UsuarioService(
            FabricacionesVidalEirlBdContext context,
            ILogger<UsuarioService> logger,
            IAuditoriaService auditoriaService)
        {
            _context = context;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        // ================================================================
        // GET - USUARIOS
        // ================================================================

        public async Task<IEnumerable<UsuarioListDto>> GetUsuariosAsync()
        {
            try
            {
                return await _context.Usuarios
                    .Include(u => u.IdRolNavigation)
                    .OrderByDescending(u => u.FechaCreacion)
                    .Select(u => new UsuarioListDto
                    {
                        IdUsuario = u.IdUsuario,
                        NombreCompleto = u.NombreCompleto,
                        NombreUsuario = u.NombreUsuario,
                        CorreoElectronico = u.CorreoElectronico,
                        NombreRol = u.IdRolNavigation.NombreRol,
                        IdRol = u.IdRol,
                        Activo = u.Activo,
                        FechaCreacion = u.FechaCreacion,
                        UltimoAcceso = u.UltimoAcceso,
                        IntentosFallidos = u.IntentosFallidos,
                        BloqueadoHasta = u.BloqueadoHasta
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetUsuariosAsync");
                return new List<UsuarioListDto>();
            }
        }

        public async Task<UsuarioDetailDto?> GetUsuarioByIdAsync(int id)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .Include(u => u.IdRolNavigation)
                    .FirstOrDefaultAsync(u => u.IdUsuario == id);

                if (usuario == null) return null;

                var totalProyectos = await _context.Proyectos
                    .CountAsync(p => p.IdUsuarioResponsable == id);

                var totalMovimientos = await _context.KardexInventarios
                    .CountAsync(k => k.IdUsuario == id);

                var totalCompras = await _context.Compras
                    .CountAsync(c => c.IdUsuario == id);

                var ultimaActividad = await _context.Auditoria
                    .Where(a => a.IdUsuario == id)
                    .OrderByDescending(a => a.FechaHora)
                    .Select(a => (DateTime?)a.FechaHora)
                    .FirstOrDefaultAsync();

                var sesionesActivas = await _context.AuthTokens
                    .CountAsync(t => t.UsuarioId == id && t.Activo);

                return new UsuarioDetailDto
                {
                    IdUsuario = usuario.IdUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    NombreUsuario = usuario.NombreUsuario,
                    CorreoElectronico = usuario.CorreoElectronico,
                    NombreRol = usuario.IdRolNavigation.NombreRol,
                    IdRol = usuario.IdRol,
                    Activo = usuario.Activo,
                    FechaCreacion = usuario.FechaCreacion,
                    UltimoAcceso = usuario.UltimoAcceso,
                    IntentosFallidos = usuario.IntentosFallidos,
                    BloqueadoHasta = usuario.BloqueadoHasta,
                    TotalProyectosCreados = totalProyectos,
                    TotalMovimientosKardex = totalMovimientos,
                    TotalComprasRegistradas = totalCompras,
                    UltimaActividad = ultimaActividad,
                    SesionesActivas = sesionesActivas
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetUsuarioByIdAsync para ID {Id}", id);
                return null;
            }
        }

        public async Task<UsuarioListDto?> GetUsuarioByUsernameAsync(string username)
        {
            try
            {
                var usuario = await _context.Usuarios
                    .Include(u => u.IdRolNavigation)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == username);

                if (usuario == null) return null;

                return new UsuarioListDto
                {
                    IdUsuario = usuario.IdUsuario,
                    NombreCompleto = usuario.NombreCompleto,
                    NombreUsuario = usuario.NombreUsuario,
                    CorreoElectronico = usuario.CorreoElectronico,
                    NombreRol = usuario.IdRolNavigation.NombreRol,
                    IdRol = usuario.IdRol,
                    Activo = usuario.Activo,
                    FechaCreacion = usuario.FechaCreacion,
                    UltimoAcceso = usuario.UltimoAcceso,
                    IntentosFallidos = usuario.IntentosFallidos,
                    BloqueadoHasta = usuario.BloqueadoHasta
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetUsuarioByUsernameAsync para '{Username}'", username);
                return null;
            }
        }

        // ================================================================
        // CREATE - USUARIO
        // ================================================================

        public async Task<(bool Success, string Message, int? IdUsuario)> CreateUsuarioAsync(UsuarioCreateDto dto)
        {
            try
            {
                // ✅ Validar nombre de usuario único
                if (await UsuarioExistsAsync(dto.NombreUsuario))
                {
                    return (false, "El nombre de usuario ya está en uso", null);
                }

                // ✅ Validar correo único (si se proporciona)
                if (!string.IsNullOrEmpty(dto.CorreoElectronico))
                {
                    if (await CorreoExistsAsync(dto.CorreoElectronico))
                    {
                        return (false, "El correo electrónico ya está en uso por otro usuario", null);
                    }
                }

                // ✅ Validar contraseña
                if (dto.Contrasena.Length < 6)
                {
                    return (false, "La contraseña debe tener al menos 6 caracteres", null);
                }

                // ✅ Validar rol
                var rol = await _context.Roles
                    .FirstOrDefaultAsync(r => r.IdRol == dto.IdRol && r.Activo);

                if (rol == null)
                {
                    return (false, "El rol seleccionado no existe o está inactivo", null);
                }

                var usuario = new Models.Usuarios
                {
                    NombreCompleto = dto.NombreCompleto.Trim(),
                    NombreUsuario = dto.NombreUsuario.Trim(),
                    CorreoElectronico = string.IsNullOrEmpty(dto.CorreoElectronico) ? null : dto.CorreoElectronico.Trim(),
                    ContrasenaHash = PasswordHelper.HashPassword(dto.Contrasena),
                    IdRol = dto.IdRol,
                    Activo = dto.Activo,
                    FechaCreacion = DateTime.Now,
                    IntentosFallidos = 0,
                    BloqueadoHasta = null
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // ✅ Auditoría
                await _auditoriaService.RegistrarAsync(
                    null,
                    "Seguridad.Usuarios",
                    "INSERT",
                    usuario.IdUsuario.ToString(),
                    null,
                    JsonSerializer.Serialize(new { usuario.NombreUsuario, usuario.NombreCompleto })
                );

                _logger.LogInformation("Usuario creado: '{NombreUsuario}' (ID {Id})", usuario.NombreUsuario, usuario.IdUsuario);
                return (true, "Usuario creado exitosamente", usuario.IdUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CreateUsuarioAsync para '{NombreUsuario}'", dto.NombreUsuario);
                return (false, "Ocurrió un error al crear el usuario", null);
            }
        }

        // ================================================================
        // UPDATE - USUARIO
        // ================================================================

        public async Task<(bool Success, string Message)> UpdateUsuarioAsync(UsuarioEditDto dto)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(dto.IdUsuario);
                if (usuario == null)
                {
                    return (false, "Usuario no encontrado");
                }

                // ✅ Validar duplicado de nombre de usuario (excluyendo el actual)
                if (await UsuarioExistsAsync(dto.NombreUsuario, dto.IdUsuario))
                {
                    return (false, "El nombre de usuario ya está en uso por otro usuario");
                }

                // ✅ Validar correo único (excluyendo el actual)
                if (!string.IsNullOrEmpty(dto.CorreoElectronico))
                {
                    if (await CorreoExistsAsync(dto.CorreoElectronico, dto.IdUsuario))
                    {
                        return (false, "El correo electrónico ya está en uso por otro usuario");
                    }
                }

                // ✅ Validar rol
                var rol = await _context.Roles
                    .FirstOrDefaultAsync(r => r.IdRol == dto.IdRol && r.Activo);

                if (rol == null)
                {
                    return (false, "El rol seleccionado no existe o está inactivo");
                }

                var valoresAnteriores = JsonSerializer.Serialize(new
                {
                    usuario.NombreCompleto,
                    usuario.CorreoElectronico,
                    usuario.IdRol,
                    usuario.Activo
                });

                usuario.NombreCompleto = dto.NombreCompleto.Trim();
                usuario.CorreoElectronico = string.IsNullOrEmpty(dto.CorreoElectronico) ? null : dto.CorreoElectronico.Trim();
                usuario.IdRol = dto.IdRol;
                usuario.Activo = dto.Activo;

                await _context.SaveChangesAsync();

                var valoresNuevos = JsonSerializer.Serialize(new
                {
                    usuario.NombreCompleto,
                    usuario.CorreoElectronico,
                    usuario.IdRol,
                    usuario.Activo
                });

                // ✅ Auditoría
                await _auditoriaService.RegistrarAsync(
                    null,
                    "Seguridad.Usuarios",
                    "UPDATE",
                    usuario.IdUsuario.ToString(),
                    valoresAnteriores,
                    valoresNuevos
                );

                _logger.LogInformation("Usuario actualizado: '{NombreUsuario}' (ID {Id})", usuario.NombreUsuario, usuario.IdUsuario);
                return (true, "Usuario actualizado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UpdateUsuarioAsync para ID {Id}", dto.IdUsuario);
                return (false, "Ocurrió un error al actualizar el usuario");
            }
        }

        // ================================================================
        // TOGGLE ACTIVO
        // ================================================================

        public async Task<(bool Success, string Message)> ToggleActivoAsync(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return (false, "Usuario no encontrado");
                }

                usuario.Activo = !usuario.Activo;
                usuario.UltimoAcceso = DateTime.Now;  // ✅ Actualizar último acceso
                await _context.SaveChangesAsync();

                // Si se desactiva, cerrar todas sus sesiones
                if (!usuario.Activo)
                {
                    var tokens = await _context.AuthTokens
                        .Where(t => t.UsuarioId == id && t.Activo)
                        .ToListAsync();

                    foreach (var token in tokens)
                    {
                        token.Activo = false;
                    }
                    await _context.SaveChangesAsync();
                }

                // ✅ Auditoría
                await _auditoriaService.RegistrarAsync(
                    null,
                    "Seguridad.Usuarios",
                    "UPDATE",
                    usuario.IdUsuario.ToString(),
                    JsonSerializer.Serialize(new { Activo = !usuario.Activo }),
                    JsonSerializer.Serialize(new { Activo = usuario.Activo })
                );

                var estado = usuario.Activo ? "activado" : "desactivado";
                _logger.LogInformation("Usuario {Estado}: '{NombreUsuario}' (ID {Id})", estado, usuario.NombreUsuario, usuario.IdUsuario);
                return (true, $"Usuario {estado} exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ToggleActivoAsync para ID {Id}", id);
                return (false, "Ocurrió un error al cambiar el estado del usuario");
            }
        }

        // ================================================================
        // DELETE - USUARIO
        // ================================================================

        public async Task<(bool Success, string Message)> DeleteUsuarioAsync(int id)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return (false, "Usuario no encontrado");
                }

                // Verificar si tiene dependencias
                var tieneProyectos = await _context.Proyectos
                    .AnyAsync(p => p.IdUsuarioResponsable == id);

                if (tieneProyectos)
                {
                    return (false, "No se puede eliminar un usuario que tiene proyectos asociados");
                }

                var tieneCompras = await _context.Compras
                    .AnyAsync(c => c.IdUsuario == id);

                if (tieneCompras)
                {
                    return (false, "No se puede eliminar un usuario que tiene compras registradas");
                }

                var tieneKardex = await _context.KardexInventarios
                    .AnyAsync(k => k.IdUsuario == id);

                if (tieneKardex)
                {
                    return (false, "No se puede eliminar un usuario que tiene movimientos de kardex");
                }

                // ✅ Cerrar todas las sesiones del usuario antes de eliminar
                var tokens = await _context.AuthTokens
                    .Where(t => t.UsuarioId == id)
                    .ToListAsync();

                if (tokens.Any())
                {
                    _context.AuthTokens.RemoveRange(tokens);
                }

                // ✅ Eliminar auditoría asociada (opcional, o solo marcar)
                // No se elimina auditoría por trazabilidad

                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                // ✅ Auditoría
                await _auditoriaService.RegistrarAsync(
                    null,
                    "Seguridad.Usuarios",
                    "DELETE",
                    id.ToString(),
                    JsonSerializer.Serialize(new { usuario.NombreUsuario }),
                    null
                );

                _logger.LogInformation("Usuario eliminado: '{NombreUsuario}' (ID {Id})", usuario.NombreUsuario, usuario.IdUsuario);
                return (true, "Usuario eliminado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DeleteUsuarioAsync para ID {Id}", id);
                return (false, "Ocurrió un error al eliminar el usuario");
            }
        }

        // ================================================================
        // VALIDACIONES
        // ================================================================

        public async Task<bool> UsuarioExistsAsync(string nombreUsuario)
        {
            try
            {
                return await _context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UsuarioExistsAsync para '{NombreUsuario}'", nombreUsuario);
                return false;
            }
        }

        public async Task<bool> UsuarioExistsAsync(string nombreUsuario, int excludeId)
        {
            try
            {
                return await _context.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario && u.IdUsuario != excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UsuarioExistsAsync para '{NombreUsuario}' excluyendo ID {ExcludeId}", nombreUsuario, excludeId);
                return false;
            }
        }

        public async Task<bool> CorreoExistsAsync(string correo)
        {
            try
            {
                if (string.IsNullOrEmpty(correo)) return false;
                return await _context.Usuarios.AnyAsync(u => u.CorreoElectronico == correo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CorreoExistsAsync para '{Correo}'", correo);
                return false;
            }
        }

        public async Task<bool> CorreoExistsAsync(string correo, int excludeId)
        {
            try
            {
                if (string.IsNullOrEmpty(correo)) return false;
                return await _context.Usuarios.AnyAsync(u => u.CorreoElectronico == correo && u.IdUsuario != excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CorreoExistsAsync para '{Correo}' excluyendo ID {ExcludeId}", correo, excludeId);
                return false;
            }
        }

        // ================================================================
        // ROLES
        // ================================================================

        public async Task<IEnumerable<UsuarioRolDto>> GetRolesAsync()
        {
            try
            {
                return await _context.Roles
                    .Where(r => r.Activo && (r.NombreRol == "Administrador" || r.NombreRol == "Jefe de Almacén" || r.NombreRol == "Ingeniero"))
                    .OrderBy(r => r.NombreRol)
                    .Select(r => new UsuarioRolDto
                    {
                        IdRol = r.IdRol,
                        NombreRol = r.NombreRol,
                        Descripcion = r.Descripcion,
                        Activo = r.Activo
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetRolesAsync");
                return new List<UsuarioRolDto>();
            }
        }

        // ================================================================
        // ESTADÍSTICAS
        // ================================================================

        public async Task<UsuarioEstadisticasDto> GetEstadisticasAsync()
        {
            try
            {
                var totalUsuarios = await _context.Usuarios.CountAsync();
                var usuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo);
                var usuariosInactivos = totalUsuarios - usuariosActivos;

                var hace5Minutos = DateTime.Now.AddMinutes(-5);
                var usuariosEnLinea = await _context.AuthTokens
                    .Where(t => t.Activo && t.FechaCreacion >= hace5Minutos)
                    .Select(t => t.UsuarioId)
                    .Distinct()
                    .CountAsync();

                var usuariosBloqueados = await _context.Usuarios
                    .CountAsync(u => u.BloqueadoHasta.HasValue && u.BloqueadoHasta.Value > DateTime.Now);

                var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var nuevosUsuariosMes = await _context.Usuarios
                    .CountAsync(u => u.FechaCreacion >= inicioMes);

                var usuariosPorRol = await _context.Usuarios
                    .GroupBy(u => u.IdRolNavigation.NombreRol)
                    .Select(g => new { Rol = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Rol, x => x.Count);

                var ultimoUsuario = await _context.Usuarios
                    .OrderByDescending(u => u.FechaCreacion)
                    .Select(u => (DateTime?)u.FechaCreacion)
                    .FirstOrDefaultAsync();

                var ultimoAccesoGeneral = await _context.Usuarios
                    .OrderByDescending(u => u.UltimoAcceso)
                    .Select(u => (DateTime?)u.UltimoAcceso)
                    .FirstOrDefaultAsync();

                return new UsuarioEstadisticasDto
                {
                    TotalUsuarios = totalUsuarios,
                    UsuariosActivos = usuariosActivos,
                    UsuariosInactivos = usuariosInactivos,
                    UsuariosEnLinea = usuariosEnLinea,
                    UsuariosBloqueados = usuariosBloqueados,
                    NuevosUsuariosMes = nuevosUsuariosMes,
                    UsuariosPorRol = usuariosPorRol,
                    UltimoUsuarioRegistrado = ultimoUsuario,
                    UltimoAccesoGeneral = ultimoAccesoGeneral
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetEstadisticasAsync");
                return new UsuarioEstadisticasDto();
            }
        }
    }
}
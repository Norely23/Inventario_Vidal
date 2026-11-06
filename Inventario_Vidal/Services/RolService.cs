using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventario_Vidal.Services
{
    public class RolService : IRolService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<RolService> _logger;
        private readonly IAuditoriaService _auditoriaService;

        public RolService(
            FabricacionesVidalEirlBdContext context,
            ILogger<RolService> logger,
            IAuditoriaService auditoriaService)
        {
            _context = context;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        // ================================================================
        // GET - ROLES
        // ================================================================

        public async Task<IEnumerable<RolListDto>> GetRolesAsync()
        {
            try
            {
                return await _context.Roles
                    .Where(r => (r.NombreRol == "Administrador" || r.NombreRol == "Jefe de Almacén" || r.NombreRol == "Ingeniero"))
                    .OrderBy(r => r.NombreRol)
                    .Select(r => new RolListDto
                    {
                        IdRol = r.IdRol,
                        NombreRol = r.NombreRol,
                        Descripcion = r.Descripcion,
                        Activo = r.Activo,
                        FechaCreacion = r.FechaCreacion,
                        CantidadUsuarios = _context.Usuarios.Count(u => u.IdRol == r.IdRol)
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetRolesAsync");
                return new List<RolListDto>();
            }
        }

        public async Task<RolListDto?> GetRolByIdAsync(int id)
        {
            try
            {
                var rol = await _context.Roles
                    .FirstOrDefaultAsync(r => r.IdRol == id);

                if (rol == null) return null;

                var cantidadUsuarios = await _context.Usuarios
                    .CountAsync(u => u.IdRol == id);

                return new RolListDto
                {
                    IdRol = rol.IdRol,
                    NombreRol = rol.NombreRol,
                    Descripcion = rol.Descripcion,
                    Activo = rol.Activo,
                    FechaCreacion = rol.FechaCreacion,
                    CantidadUsuarios = cantidadUsuarios
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetRolByIdAsync para ID {Id}", id);
                return null;
            }
        }

        public async Task<IEnumerable<RolSelectDto>> GetRolesSelectAsync()
        {
            try
            {
                return await _context.Roles
                    .Where(r => r.Activo && (r.NombreRol == "Administrador" || r.NombreRol == "Jefe de Almacén" || r.NombreRol == "Ingeniero"))
                    .OrderBy(r => r.NombreRol)
                    .Select(r => new RolSelectDto
                    {
                        IdRol = r.IdRol,
                        NombreRol = r.NombreRol,
                        Activo = r.Activo
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetRolesSelectAsync");
                return new List<RolSelectDto>();
            }
        }

        // ================================================================
        // CREATE - ROL
        // ================================================================

        public Task<(bool Success, string Message)> CreateRolAsync(RolCreateDto dto)
        {
            return Task.FromResult((false, "Los tres roles del sistema son fijos. Configure sus permisos desde el módulo de seguridad."));
        }

        // ================================================================
        // UPDATE - ROL
        // ================================================================

        public async Task<(bool Success, string Message)> UpdateRolAsync(RolEditDto dto)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(dto.IdRol);
                if (rol == null)
                {
                    return (false, "Rol no encontrado");
                }

                if (rol.NombreRol != "Administrador" && rol.NombreRol != "Jefe de Almacén" && rol.NombreRol != "Ingeniero")
                    return (false, "El rol no pertenece al catálogo autorizado.");

                if (!string.Equals(dto.NombreRol?.Trim(), rol.NombreRol, StringComparison.Ordinal) || !dto.Activo)
                    return (false, "Los roles del sistema no se pueden renombrar ni desactivar.");

                // Validar duplicado de nombre (excluyendo el actual)
                if (await RolExistsAsync(dto.NombreRol, dto.IdRol))
                {
                    return (false, "Ya existe otro rol con este nombre");
                }

                // ✅ VALORES ANTERIORES PARA AUDITORÍA
                var valoresAnteriores = JsonSerializer.Serialize(new
                {
                    rol.NombreRol,
                    rol.Descripcion,
                    rol.Activo
                });

                rol.NombreRol = dto.NombreRol.Trim();
                rol.Descripcion = string.IsNullOrEmpty(dto.Descripcion) ? null : dto.Descripcion.Trim();
                rol.Activo = dto.Activo;

                await _context.SaveChangesAsync();

                var valoresNuevos = JsonSerializer.Serialize(new
                {
                    rol.NombreRol,
                    rol.Descripcion,
                    rol.Activo
                });

                // ✅ Auditoría
                await _auditoriaService.RegistrarAsync(
                    null,
                    "Seguridad.Roles",
                    "UPDATE",
                    rol.IdRol.ToString(),
                    valoresAnteriores,
                    valoresNuevos
                );

                _logger.LogInformation("Rol actualizado: '{NombreRol}' (ID {Id})", rol.NombreRol, rol.IdRol);
                return (true, "Rol actualizado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UpdateRolAsync para ID {Id}", dto.IdRol);
                return (false, "Ocurrió un error al actualizar el rol");
            }
        }

        // ================================================================
        // TOGGLE ACTIVO
        // ================================================================

        public async Task<(bool Success, string Message)> ToggleActivoAsync(int id)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(id);
                if (rol == null)
                {
                    return (false, "Rol no encontrado");
                }

                if (rol.NombreRol == "Administrador" || rol.NombreRol == "Jefe de Almacén" || rol.NombreRol == "Ingeniero")
                    return (false, "Los roles oficiales del sistema no pueden desactivarse.");

                // Verificar si tiene usuarios asociados antes de desactivar
                if (rol.Activo)
                {
                    var tieneUsuarios = await _context.Usuarios
                        .AnyAsync(u => u.IdRol == id && u.Activo);

                    if (tieneUsuarios)
                    {
                        return (false, "No se puede desactivar un rol que tiene usuarios activos asociados");
                    }
                }

                var estadoAnterior = rol.Activo;
                rol.Activo = !rol.Activo;
                await _context.SaveChangesAsync();

                // ✅ Auditoría
                await _auditoriaService.RegistrarAsync(
                    null,
                    "Seguridad.Roles",
                    "UPDATE",
                    rol.IdRol.ToString(),
                    JsonSerializer.Serialize(new { Activo = estadoAnterior }),
                    JsonSerializer.Serialize(new { Activo = rol.Activo })
                );

                var estado = rol.Activo ? "activado" : "desactivado";
                _logger.LogInformation("Rol {Estado}: '{NombreRol}' (ID {Id})", estado, rol.NombreRol, rol.IdRol);
                return (true, $"Rol {estado} exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ToggleActivoAsync para ID {Id}", id);
                return (false, "Ocurrió un error al cambiar el estado del rol");
            }
        }

        // ================================================================
        // DELETE - ROL
        // ================================================================

        public async Task<(bool Success, string Message)> DeleteRolAsync(int id)
        {
            try
            {
                var rol = await _context.Roles.FindAsync(id);
                if (rol == null)
                {
                    return (false, "Rol no encontrado");
                }

                if (rol.NombreRol == "Administrador" || rol.NombreRol == "Jefe de Almacén" || rol.NombreRol == "Ingeniero")
                    return (false, "Los roles oficiales del sistema no pueden eliminarse.");

                // Verificar si tiene usuarios asociados
                var tieneUsuarios = await _context.Usuarios
                    .AnyAsync(u => u.IdRol == id);

                if (tieneUsuarios)
                {
                    return (false, "No se puede eliminar un rol que tiene usuarios asociados");
                }

                var valoresAnteriores = JsonSerializer.Serialize(new { rol.NombreRol });

                _context.Roles.Remove(rol);
                await _context.SaveChangesAsync();

                // ✅ Auditoría
                await _auditoriaService.RegistrarAsync(
                    null,
                    "Seguridad.Roles",
                    "DELETE",
                    id.ToString(),
                    valoresAnteriores,
                    null
                );

                _logger.LogInformation("Rol eliminado: '{NombreRol}' (ID {Id})", rol.NombreRol, rol.IdRol);
                return (true, "Rol eliminado exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DeleteRolAsync para ID {Id}", id);
                return (false, "Ocurrió un error al eliminar el rol");
            }
        }

        // ================================================================
        // VALIDACIONES
        // ================================================================

        public async Task<bool> RolExistsAsync(string nombreRol)
        {
            try
            {
                return await _context.Roles.AnyAsync(r => r.NombreRol == nombreRol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RolExistsAsync para '{NombreRol}'", nombreRol);
                return false;
            }
        }

        public async Task<bool> RolExistsAsync(string nombreRol, int excludeId)
        {
            try
            {
                return await _context.Roles.AnyAsync(r => r.NombreRol == nombreRol && r.IdRol != excludeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RolExistsAsync para '{NombreRol}' excluyendo ID {ExcludeId}", nombreRol, excludeId);
                return false;
            }
        }

        // ================================================================
        // PERMISOS
        // ================================================================

        public async Task<List<PermisoDto>> GetPermisosByRolAsync(int rolId)
        {
            try
            {
                var rolExiste = await _context.Roles
                    .AsNoTracking()
                    .AnyAsync(r => r.IdRol == rolId);

                if (!rolExiste)
                    return new List<PermisoDto>();

                var permisosAsignados = await _context.Roles
                    .AsNoTracking()
                    .Where(r => r.IdRol == rolId)
                    .SelectMany(r => r.IdPermisos.Select(p => p.IdPermiso))
                    .ToListAsync();

                var permisos = await _context.Permisos
                    .AsNoTracking()
                    .Where(p => p.Activo)
                    .OrderBy(p => p.Modulo)
                    .ThenBy(p => p.Codigo)
                    .ToListAsync();

                var asignados = permisosAsignados.ToHashSet();

                return permisos
                    .Select(p => new PermisoDto
                    {
                        IdPermiso = p.IdPermiso,
                        Nombre = ObtenerNombrePermiso(p.Codigo, p.Descripcion),
                        Descripcion = $"Código: {p.Codigo}",
                        Modulo = p.Modulo,
                        Icono = ObtenerIconoModulo(p.Modulo),
                        Asignado = asignados.Contains(p.IdPermiso),
                        Categoria = ObtenerCategoriaPermiso(p.Codigo)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error en GetPermisosByRolAsync para Rol ID {RolId}",
                    rolId);

                return new List<PermisoDto>();
            }
        }

        public async Task<(bool Success, string Message)> UpdatePermisosAsync(
            int rolId,
            List<int> permisosIds)
        {
            try
            {
                var idsSolicitados = (permisosIds ?? new List<int>())
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var rol = await _context.Roles
                    .Include(r => r.IdPermisos)
                    .FirstOrDefaultAsync(r => r.IdRol == rolId);

                if (rol == null)
                    return (false, "Rol no encontrado");

                var permisosValidos = await _context.Permisos
                    .Where(p =>
                        p.Activo &&
                        idsSolicitados.Contains(p.IdPermiso))
                    .ToListAsync();

                if (permisosValidos.Count != idsSolicitados.Count)
                {
                    return (
                        false,
                        "Uno o más permisos seleccionados no existen o están inactivos");
                }

                var permisosAnteriores = rol.IdPermisos
                    .Select(p => p.IdPermiso)
                    .OrderBy(id => id)
                    .ToList();

                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                try
                {
                    rol.IdPermisos.Clear();

                    foreach (var permiso in permisosValidos)
                        rol.IdPermisos.Add(permiso);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

                var permisosNuevos = permisosValidos
                    .Select(p => p.IdPermiso)
                    .OrderBy(id => id)
                    .ToList();

                try
                {
                    await _auditoriaService.RegistrarAsync(
                        null,
                        "Seguridad.RolPermiso",
                        "UPDATE",
                        rolId.ToString(),
                        JsonSerializer.Serialize(new
                        {
                            Rol = rol.NombreRol,
                            Permisos = permisosAnteriores
                        }),
                        JsonSerializer.Serialize(new
                        {
                            Rol = rol.NombreRol,
                            Permisos = permisosNuevos
                        }));
                }
                catch (Exception auditEx)
                {
                    _logger.LogWarning(
                        auditEx,
                        "Los permisos del rol {RolId} se guardaron, pero no se pudo registrar la auditoría",
                        rolId);
                }

                _logger.LogInformation(
                    "Permisos actualizados para el rol {RolId}. Antes: {Antes}. Ahora: {Ahora}",
                    rolId,
                    permisosAnteriores.Count,
                    permisosNuevos.Count);

                return (
                    true,
                    $"Permisos actualizados exitosamente ({permisosNuevos.Count} asignados)");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error en UpdatePermisosAsync para Rol ID {RolId}",
                    rolId);

                return (
                    false,
                    "Ocurrió un error al actualizar los permisos");
            }
        }

        private static string ObtenerNombrePermiso(
            string codigo,
            string? descripcion)
        {
            if (!string.IsNullOrWhiteSpace(descripcion))
                return descripcion.Trim();

            if (string.IsNullOrWhiteSpace(codigo))
                return "Permiso";

            return codigo
                .Replace(".", " ")
                .Replace("_", " ")
                .ToLowerInvariant()
                .Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(p =>
                    char.ToUpperInvariant(p[0]) +
                    p[1..])
                .Aggregate(
                    (actual, siguiente) =>
                        $"{actual} {siguiente}");
        }

        private static string ObtenerCategoriaPermiso(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return "General";

            var parte = codigo
                .Split(
                    '.',
                    StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(parte))
                return "General";

            parte = parte
                .Replace("_", " ")
                .ToLowerInvariant();

            return char.ToUpperInvariant(parte[0]) +
                parte[1..];
        }

        private static string ObtenerIconoModulo(string modulo)
        {
            return modulo?.Trim().ToLowerInvariant() switch
            {
                "seguridad" => "shield",
                "inventario" => "box",
                "compras" => "shopping-cart",
                "proyectos" => "folder",
                "maquinaria" => "settings",
                "reportes" => "chart-bar",
                "configuracion" => "settings",
                "configuración" => "settings",
                _ => "key"
            };
        }

        // ================================================================
        // MÉTODOS ADICIONALES PARA COMPLETAR LA INTERFAZ
        // ================================================================

        public async Task<RolEstadisticasDto> GetEstadisticasAsync()
        {
            try
            {
                var roles = await _context.Roles.ToListAsync();
                var total = roles.Count;
                var activos = roles.Count(r => r.Activo);

                var usuariosPorRol = await _context.Usuarios
                    .GroupBy(u => u.IdRolNavigation.NombreRol)
                    .Select(g => new { Rol = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Rol, x => x.Count);

                return new RolEstadisticasDto
                {
                    TotalRoles = total,
                    RolesActivos = activos,
                    RolesInactivos = total - activos,
                    TotalUsuarios = await _context.Usuarios.CountAsync(),
                    UsuariosPorRol = usuariosPorRol,
                    UltimoRolCreado = roles
                        .OrderByDescending(r => r.FechaCreacion)
                        .Select(r => (DateTime?)r.FechaCreacion)
                        .FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetEstadisticasAsync");
                return new RolEstadisticasDto();
            }
        }
    }
}
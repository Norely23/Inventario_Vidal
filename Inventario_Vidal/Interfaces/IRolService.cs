using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IRolService
    {
        /// <summary>
        /// Obtiene todos los roles del sistema
        /// </summary>
        Task<IEnumerable<RolListDto>> GetRolesAsync();

        /// <summary>
        /// Obtiene un rol por su ID
        /// </summary>
        Task<RolListDto?> GetRolByIdAsync(int id);

        /// <summary>
        /// Crea un nuevo rol en el sistema
        /// </summary>
        Task<(bool Success, string Message)> CreateRolAsync(RolCreateDto dto);

        /// <summary>
        /// Actualiza un rol existente
        /// </summary>
        Task<(bool Success, string Message)> UpdateRolAsync(RolEditDto dto);

        /// <summary>
        /// Cambia el estado (Activo/Inactivo) de un rol
        /// </summary>
        Task<(bool Success, string Message)> ToggleActivoAsync(int id);

        /// <summary>
        /// Elimina un rol (si no tiene usuarios asignados)
        /// </summary>
        Task<(bool Success, string Message)> DeleteRolAsync(int id);

        /// <summary>
        /// Verifica si un nombre de rol ya existe
        /// </summary>
        Task<bool> RolExistsAsync(string nombreRol);

        /// <summary>
        /// Verifica si un nombre de rol ya existe (excluyendo un ID)
        /// </summary>
        Task<bool> RolExistsAsync(string nombreRol, int excludeId);

        /// <summary>
        /// Obtiene roles para dropdown (solo activos)
        /// </summary>
        Task<IEnumerable<RolSelectDto>> GetRolesSelectAsync();

        /// <summary>
        /// Obtiene los permisos asignados a un rol
        /// </summary>
        Task<List<PermisoDto>> GetPermisosByRolAsync(int rolId);

        /// <summary>
        /// Actualiza los permisos de un rol
        /// </summary>
        Task<(bool Success, string Message)> UpdatePermisosAsync(int rolId, List<int> permisosIds);

        /// <summary>
        /// Obtiene estadísticas de roles
        /// </summary>
        Task<RolEstadisticasDto> GetEstadisticasAsync();
    }
}
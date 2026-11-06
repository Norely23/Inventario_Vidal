using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IUsuarioService
    {
        /// <summary>
        /// Obtiene todos los usuarios del sistema
        /// </summary>
        Task<IEnumerable<UsuarioListDto>> GetUsuariosAsync();

        /// <summary>
        /// Obtiene un usuario por su ID
        /// </summary>
        Task<UsuarioDetailDto?> GetUsuarioByIdAsync(int id);

        /// <summary>
        /// Obtiene un usuario por su nombre de usuario
        /// </summary>
        Task<UsuarioListDto?> GetUsuarioByUsernameAsync(string username);

        /// <summary>
        /// Crea un nuevo usuario en el sistema
        /// </summary>
        Task<(bool Success, string Message, int? IdUsuario)> CreateUsuarioAsync(UsuarioCreateDto dto);

        /// <summary>
        /// Actualiza la información de un usuario existente
        /// </summary>
        Task<(bool Success, string Message)> UpdateUsuarioAsync(UsuarioEditDto dto);

        /// <summary>
        /// Cambia el estado (Activo/Inactivo) de un usuario
        /// </summary>
        Task<(bool Success, string Message)> ToggleActivoAsync(int id);

        /// <summary>
        /// Elimina un usuario del sistema (si no tiene dependencias)
        /// </summary>
        Task<(bool Success, string Message)> DeleteUsuarioAsync(int id);

        /// <summary>
        /// Verifica si un nombre de usuario ya existe
        /// </summary>
        Task<bool> UsuarioExistsAsync(string nombreUsuario);

        /// <summary>
        /// Verifica si un nombre de usuario ya existe (excluyendo un ID)
        /// </summary>
        Task<bool> UsuarioExistsAsync(string nombreUsuario, int excludeId);

        /// <summary>
        /// Obtiene todos los roles activos para dropdowns
        /// </summary>
        Task<IEnumerable<UsuarioRolDto>> GetRolesAsync();

        /// <summary>
        /// Obtiene estadísticas de usuarios para el dashboard
        /// </summary>
        Task<UsuarioEstadisticasDto> GetEstadisticasAsync();
    }
}
using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IAuditoriaService
    {
        /// <summary>
        /// Registra una acción en la auditoría
        /// </summary>
        Task RegistrarAsync(
            int? usuarioId,
            string nombreTabla,
            string accion,
            string? idRegistroAfectado,
            string? valoresAnteriores = null,
            string? valoresNuevos = null,
            string? ip = null,
            string? userAgent = null);

        /// <summary>
        /// Obtiene el historial de auditoría con filtros
        /// </summary>
        Task<List<AuditoriaDto>> GetAuditoriaAsync(
            string? tabla = null,
            string? accion = null,
            int? usuarioId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Obtiene el historial de cambios de un registro específico
        /// </summary>
        Task<List<AuditoriaDto>> GetHistorialRegistroAsync(
            string tabla,
            string idRegistro,
            int? usuarioId = null,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Obtiene las tablas disponibles en auditoría
        /// </summary>
        Task<List<string>> GetTablasAuditadasAsync();

        /// <summary>
        /// Obtiene las acciones disponibles en auditoría
        /// </summary>
        Task<List<string>> GetAccionesAuditadasAsync();

        /// <summary>
        /// Obtiene un resumen de auditoría
        /// </summary>
        Task<AuditoriaResumenDto> GetResumenAsync();

        /// <summary>
        /// Obtiene los usuarios con actividad en auditoría
        /// </summary>
        Task<Dictionary<int, string>> GetUsuariosConActividadAsync();

        /// <summary>
        /// Obtiene el conteo de registros por día (últimos N días)
        /// </summary>
        Task<Dictionary<DateTime, int>> GetRegistrosPorDiaAsync(int dias = 7);

        /// <summary>
        /// Obtiene las tablas más auditadas
        /// </summary>
        Task<Dictionary<string, int>> GetTablasMasAuditadasAsync(int cantidad = 10);
    }
}
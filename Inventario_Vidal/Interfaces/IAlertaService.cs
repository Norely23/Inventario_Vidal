using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IAlertaService
    {
        /// <summary>
        /// Obtiene todas las alertas de stock bajo
        /// </summary>
        Task<IEnumerable<AlertasStockDto>> GetAlertasStockBajoAsync();

        /// <summary>
        /// Obtiene el conteo total de alertas activas
        /// </summary>
        Task<int> GetAlertasCountAsync();

        /// <summary>
        /// Obtiene alertas de devoluciones pendientes de contratistas
        /// </summary>
        Task<IEnumerable<ContratistaRecursoAsignadoDto>> GetAlertasDevolucionesContratistasAsync();

        /// <summary>
        /// Obtiene alertas de devoluciones pendientes de empleados
        /// </summary>
        Task<IEnumerable<EmpleadoRecursoAsignadoDto>> GetAlertasDevolucionesEmpleadosAsync();

        /// <summary>
        /// Obtiene alertas de préstamos de maquinaria vencidos
        /// </summary>
        Task<IEnumerable<PrestamoMaquinariaListDto>> GetAlertasPrestamosVencidosAsync();

        /// <summary>
        /// Marca una alerta como leída
        /// </summary>
        Task<bool> MarcarComoLeidaAsync(int alertaId);

        /// <summary>
        /// Marca todas las alertas como leídas
        /// </summary>
        Task<bool> MarcarTodasComoLeidasAsync();

        /// <summary>
        /// Obtiene el resumen de todas las alertas
        /// </summary>
        Task<AlertaResumenDto> GetResumenAlertasAsync();
    }
}
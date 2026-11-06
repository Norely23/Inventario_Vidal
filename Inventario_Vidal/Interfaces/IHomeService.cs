using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IHomeService
    {
        /// <summary>
        /// Obtiene los datos para el Dashboard
        /// </summary>
        Task<DashboardViewModel> GetDashboardDataAsync();

        /// <summary>
        /// Obtiene el conteo de alertas de stock bajo
        /// </summary>
        Task<int> GetAlertasStockCountAsync();

        /// <summary>
        /// Obtiene los últimos movimientos del kardex
        /// </summary>
        Task<List<UltimoMovimientoDto>> GetUltimosMovimientosAsync(int cantidad = 10);

        /// <summary>
        /// Obtiene los proyectos recientes
        /// </summary>
        Task<List<ProyectoRecienteDto>> GetProyectosRecientesAsync(int cantidad = 5);

        /// <summary>
        /// Obtiene las estadísticas rápidas para el dashboard
        /// </summary>
        Task<DashboardEstadisticasDto> GetEstadisticasRapidasAsync();

        /// <summary>
        /// Obtiene los usuarios en línea (activos en los últimos 5 minutos)
        /// </summary>
        Task<List<UsuarioActivoDto>> GetUsuariosEnLineaAsync();

        /// <summary>
        /// Obtiene las compras por mes para gráficos
        /// </summary>
        Task<IEnumerable<ComprasPorMesDto>> GetComprasPorMesAsync(int meses = 12);

        /// <summary>
        /// Obtiene los proyectos por estado para gráficos
        /// </summary>
        Task<IEnumerable<EstadoCountDto>> GetProyectosPorEstadoAsync();
    }
}
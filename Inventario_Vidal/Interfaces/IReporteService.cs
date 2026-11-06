using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    // Interfaces/IReporteService.cs
    public interface IReporteService
    {
        Task<IEnumerable<ReporteStockDto>> GetReporteStockAsync(bool soloAlertas = false);
        Task<IEnumerable<ReporteProyectoDto>> GetReporteProyectosAsync(DateTime? desde = null, DateTime? hasta = null);
        Task<IEnumerable<ReporteCompraDto>> GetReporteComprasAsync(DateTime? desde = null, DateTime? hasta = null, int? idProveedor = null);
        Task<IEnumerable<ReporteKardexDto>> GetReporteKardexAsync(int? idMaterial = null, DateTime? desde = null, DateTime? hasta = null);
        Task<IEnumerable<ReporteMaquinariaDto>> GetReporteMaquinariaAsync();
        Task<IEnumerable<ReporteClienteDto>> GetReporteClientesAsync(string search = "", string tipo = "", string orden = "proyectos");
        Task<IEnumerable<ReporteContratistaDto>> GetReporteContratistasAsync(string search = "", string especialidad = "", string orden = "horas");
        Task<IEnumerable<ReporteUtilidadDto>> GetReporteUtilidadAsync(string search = "", string resultado = "", DateTime? desde = null, DateTime? hasta = null);
        Task<EstadisticasRapidasDto> GetEstadisticasRapidasAsync();
        Task<byte[]> ExportarExcelAsync<T>(IEnumerable<T> data, string titulo, string[]? columnas = null);
        Task<IEnumerable<MaterialUsoDto>> GetMaterialesMasUtilizadosAsync(int cantidad = 10, DateTime? desde = null, DateTime? hasta = null);
        Task<IEnumerable<ReporteConsumoProyectoDto>> GetReporteConsumoProyectoAsync(int idProyecto);
    }
}
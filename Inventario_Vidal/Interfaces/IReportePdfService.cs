// Interfaces/IReportePdfService.cs
using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IReportePdfService
    {
        // Reporte de Stock
        Task<byte[]> GenerarPdfStockAsync(IEnumerable<ReporteStockDto> reporte, bool soloAlertas = false);

        // Reporte de Proyectos
        Task<byte[]> GenerarPdfProyectosAsync(IEnumerable<ReporteProyectoDto> reporte, DateTime? desde = null, DateTime? hasta = null);

        // Reporte de Compras
        Task<byte[]> GenerarPdfComprasAsync(IEnumerable<ReporteCompraDto> reporte, DateTime? desde = null, DateTime? hasta = null, int? idProveedor = null);

        // Reporte de Kardex
        Task<byte[]> GenerarPdfKardexAsync(IEnumerable<ReporteKardexDto> reporte, int? idMaterial = null, DateTime? desde = null, DateTime? hasta = null);

        // Reporte de Maquinaria
        Task<byte[]> GenerarPdfMaquinariaAsync(IEnumerable<ReporteMaquinariaDto> reporte);

        // Reporte de Clientes
        Task<byte[]> GenerarPdfClientesAsync(IEnumerable<ReporteClienteDto> reporte);

        // Reporte de Contratistas
        Task<byte[]> GenerarPdfContratistasAsync(IEnumerable<ReporteContratistaDto> reporte);

        // Reporte de Utilidad
        Task<byte[]> GenerarPdfUtilidadAsync(IEnumerable<ReporteUtilidadDto> reporte);
        Task<byte[]> GenerarPdfEquipoAsync(IEnumerable<AsignacionesPersonalListDto> personas);
        Task<byte[]> GenerarPdfAsignacionesActivasAsync(IEnumerable<AsignacionMaterialesMaterialDto> asignaciones);
        Task<byte[]> GenerarPdfAsignacionesPersonalAsync(IEnumerable<AsignacionPersonalProyectoListDto> asignaciones);
        /// <summary>
        /// Genera PDF del desglose de paquetes por material
        /// </summary>
        Task<byte[]> GenerarPdfDesglosePaquetesAsync(IEnumerable<PaqueteDesgloseMaterialDto> desglose);
    }
}
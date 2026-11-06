using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Services
{
    public class AlertaService : IAlertaService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<AlertaService> _logger;

        public AlertaService(FabricacionesVidalEirlBdContext context, ILogger<AlertaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================================================
        // ALERTAS DE STOCK BAJO
        // ================================================================

        public async Task<IEnumerable<AlertasStockDto>> GetAlertasStockBajoAsync()
        {
            try
            {
                return await _context.VwAlertasStockBajos
                    .Where(a => a.AlertaStockBajo == 1 && a.Activo == true)
                    .OrderBy(a => a.StockActual)
                    .Select(a => new AlertasStockDto
                    {
                        IdMaterial = a.IdMaterial,
                        CodigoBarras = a.CodigoBarras,
                        Nombre = a.Nombre,
                        Categoria = a.Categoria,
                        UnidadConsumo = a.UnidadConsumo,
                        StockActual = a.StockActual,
                        StockMinimo = a.StockMinimo,
                        StockMaximo = a.StockMaximo,
                        StockReservado = a.StockReservado,
                        StockDisponible = a.StockDisponible,
                        ImagenUrl = a.ImagenUrl ?? string.Empty,
                        EstadoStock = a.EstadoStock,
                        AlertaStockBajo = a.AlertaStockBajo,
                        Activo = a.Activo
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAlertasStockBajoAsync");
                return new List<AlertasStockDto>();
            }
        }

        public async Task<int> GetAlertasCountAsync()
        {
            try
            {
                return await _context.VwAlertasStockBajos
                    .CountAsync(a => a.AlertaStockBajo == 1 && a.Activo == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAlertasCountAsync");
                return 0;
            }
        }

        // ================================================================
        // COMPATIBILIDAD: ALERTAS ANTIGUAS DE DEVOLUCIÓN DE MATERIALES
        // Los materiales consumibles no tienen fecha de devolución.
        // Sus sobrantes se registran en Inventario.MaterialesRecuperados.
        // Se conservan las firmas para IAlertaService y los controladores.
        // ================================================================

        public Task<IEnumerable<ContratistaRecursoAsignadoDto>> GetAlertasDevolucionesContratistasAsync()
        {
            IEnumerable<ContratistaRecursoAsignadoDto> resultado = Array.Empty<ContratistaRecursoAsignadoDto>();
            return Task.FromResult(resultado);
        }

        public Task<IEnumerable<EmpleadoRecursoAsignadoDto>> GetAlertasDevolucionesEmpleadosAsync()
        {
            IEnumerable<EmpleadoRecursoAsignadoDto> resultado = Array.Empty<EmpleadoRecursoAsignadoDto>();
            return Task.FromResult(resultado);
        }

        // ================================================================
        // ALERTAS DE PRÉSTAMOS DE MAQUINARIA VENCIDOS
        // ================================================================

        public async Task<IEnumerable<PrestamoMaquinariaListDto>> GetAlertasPrestamosVencidosAsync()
        {
            try
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);

                var prestamos = await _context.PrestamosMaquinaria
                    .Include(p => p.IdMaquinariaNavigation)
                    .Include(p => p.IdProyectoNavigation)
                    .Include(p => p.IdContratistaNavigation)
                    .Include(p => p.IdEmpleadoNavigation)
                    .Where(p => p.FechaDevolucionReal == null &&
                                p.FechaDevolucionEstimada.HasValue &&
                                p.FechaDevolucionEstimada.Value <= hoy)
                    .OrderBy(p => p.FechaDevolucionEstimada)
                    .Select(p => new PrestamoMaquinariaListDto
                    {
                        IdPrestamo = p.IdPrestamo,
                        IdMaquinaria = p.IdMaquinaria,
                        Maquinaria = p.IdMaquinariaNavigation.Nombre,
                        CodigoMaquinaria = p.IdMaquinariaNavigation.Codigo,
                        IdProyecto = p.IdProyecto,
                        CodigoProyecto = p.IdProyectoNavigation != null ? p.IdProyectoNavigation.CodigoProyecto : null,
                        IdContratista = p.IdContratista,
                        Contratista = p.IdContratistaNavigation != null ? p.IdContratistaNavigation.NombreCompleto : null,
                        IdEmpleado = p.IdEmpleado,
                        Empleado = p.IdEmpleadoNavigation != null ? p.IdEmpleadoNavigation.NombreCompleto : null,
                        FechaPrestamo = p.FechaPrestamo,
                        FechaDevolucionEstimada = p.FechaDevolucionEstimada,
                        FechaDevolucionReal = p.FechaDevolucionReal,
                        Estado = p.Estado,
                        Observaciones = p.Observaciones
                    })
                    .ToListAsync();

                foreach (var p in prestamos)
                {
                    var prestamo = await _context.PrestamosMaquinaria
                        .FirstOrDefaultAsync(x => x.IdPrestamo == p.IdPrestamo);

                    if (prestamo != null && prestamo.Estado != "Atrasado")
                    {
                        prestamo.Estado = "Atrasado";
                    }
                }
                await _context.SaveChangesAsync();

                return prestamos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAlertasPrestamosVencidosAsync");
                return new List<PrestamoMaquinariaListDto>();
            }
        }

        // ================================================================
        // MARCAR ALERTAS COMO LEÍDAS
        // ================================================================

        public async Task<bool> MarcarComoLeidaAsync(int alertaId)
        {
            try
            {
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en MarcarComoLeidaAsync para ID {AlertaId}", alertaId);
                return false;
            }
        }

        public async Task<bool> MarcarTodasComoLeidasAsync()
        {
            try
            {
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en MarcarTodasComoLeidasAsync");
                return false;
            }
        }

        // ================================================================
        // RESUMEN DE ALERTAS
        // ================================================================

        public async Task<AlertaResumenDto> GetResumenAlertasAsync()
        {
            try
            {
                var stockBajo = await GetAlertasCountAsync();
                var devolucionesContratistas = (await GetAlertasDevolucionesContratistasAsync()).Count();
                var devolucionesEmpleados = (await GetAlertasDevolucionesEmpleadosAsync()).Count();
                var prestamosVencidos = (await GetAlertasPrestamosVencidosAsync()).Count();

                return new AlertaResumenDto
                {
                    StockBajo = stockBajo,
                    DevolucionesContratistas = devolucionesContratistas,
                    DevolucionesEmpleados = devolucionesEmpleados,
                    PrestamosVencidos = prestamosVencidos,
                    TotalAlertas = stockBajo + devolucionesContratistas + devolucionesEmpleados + prestamosVencidos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetResumenAlertasAsync");
                return new AlertaResumenDto();
            }
        }
    }
}
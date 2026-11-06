using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Services
{
    public class HomeService : IHomeService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<HomeService> _logger;

        public HomeService(FabricacionesVidalEirlBdContext context, ILogger<HomeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ================================================================
        // DASHBOARD COMPLETO
        // ================================================================

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            try
            {
                var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var hace30Dias = DateTime.Now.AddDays(-30);
                var hace5Minutos = DateTime.Now.AddMinutes(-5);

                // ============================================================
                // TARJETAS DE RESUMEN (KPI)
                // ============================================================
                var totalMateriales = await _context.Materiales.CountAsync(m => m.Activo);
                var totalProyectos = await _context.Proyectos.CountAsync();
                var totalProveedores = await _context.Proveedores.CountAsync(p => p.Activo);
                var totalClientes = await _context.Clientes.CountAsync(c => c.Activo);
                var totalContratistas = await _context.Contratistas.CountAsync(c => c.Activo);
                var totalEmpleados = await _context.Empleados.CountAsync(e => e.Activo);
                var totalMaquinarias = await _context.Maquinarias.CountAsync(m => m.Activo);
                var totalUsuarios = await _context.Usuarios.CountAsync(u => u.Activo);

                // ============================================================
                // FINANCIERO
                // ============================================================
                var comprasDelMes = await _context.Compras
                    .Where(c => c.FechaCompra >= inicioMes && c.Estado != "Anulada")
                    .SumAsync(c => c.Total);

                var comprasDelAnio = await _context.Compras
                    .Where(c => c.FechaCompra.Year == DateTime.Now.Year && c.Estado != "Anulada")
                    .SumAsync(c => c.Total);

                var proyectosActivos = await _context.Proyectos
                    .CountAsync(p => p.IdEstadoNavigation.Nombre == "En Proceso de Fabricacion" ||
                                     p.IdEstadoNavigation.Nombre == "En Proceso de Acabado" ||
                                     p.IdEstadoNavigation.Nombre == "Instalacion de Electricidad");

                var proyectosCompletados = await _context.Proyectos
                    .CountAsync(p => p.IdEstadoNavigation.Nombre == "Terminado" ||
                                     p.IdEstadoNavigation.Nombre == "Entregado");

                // ============================================================
                // GRÁFICOS - Proyectos por Estado
                // ============================================================
                var proyectosPorEstado = await _context.Proyectos
                    .GroupBy(p => p.IdEstadoNavigation.Nombre)
                    .Select(g => new EstadoCountDto
                    {
                        Estado = g.Key,
                        Cantidad = g.Count()
                    })
                    .ToListAsync();

                // ============================================================
                // GRÁFICOS - Materiales más usados (últimos 30 días)
                // ============================================================
                var materialesMasUsados = await _context.KardexInventarios
                    .Include(k => k.IdMaterialNavigation)
                    .Include(k => k.IdTipoMovimientoNavigation)
                    .Where(k => k.FechaMovimiento >= hace30Dias &&
                                (k.IdTipoMovimientoNavigation.Nombre == "Salida por Asignacion a Proyecto" ||
                                 k.IdTipoMovimientoNavigation.Nombre == "Prestamo a Contratista" ||
                                 k.IdTipoMovimientoNavigation.Nombre == "Entrega a Empleado"))
                    .GroupBy(k => new
                    {
                        k.IdMaterial,
                        k.IdMaterialNavigation.Nombre,
                        UnidadConsumo = k.IdMaterialNavigation.IdUnidadConsumoNavigation.Nombre
                    })
                    .Select(g => new MaterialUsoDto
                    {
                        IdMaterial = g.Key.IdMaterial,
                        Material = g.Key.Nombre,
                        Cantidad = g.Sum(k => k.Cantidad),
                        Unidad = g.Key.UnidadConsumo
                    })
                    .OrderByDescending(m => m.Cantidad)
                    .Take(5)
                    .ToListAsync();

                // Calcular porcentajes
                var totalUso = materialesMasUsados.Sum(m => m.Cantidad);
                if (totalUso > 0)
                {
                    foreach (var item in materialesMasUsados)
                    {
                        item.Porcentaje = Math.Round((item.Cantidad / totalUso) * 100, 1);
                    }
                }

                // ============================================================
                // GRÁFICOS - Compras por mes (últimos 12 meses)
                // ============================================================
                var comprasPorMes = await _context.Compras
                    .Where(c => c.FechaCompra >= DateTime.Now.AddMonths(-12) && c.Estado != "Anulada")
                    .GroupBy(c => new { c.FechaCompra.Year, c.FechaCompra.Month })
                    .Select(g => new ComprasPorMesDto
                    {
                        MesNumero = g.Key.Month,
                        Anio = g.Key.Year,
                        Mes = $"{g.Key.Month:00}/{g.Key.Year}",
                        Total = g.Sum(c => c.Total)
                    })
                    .OrderBy(c => c.Anio).ThenBy(c => c.MesNumero)
                    .ToListAsync();

                // ============================================================
                // ALERTAS DE STOCK BAJO
                // ✅ CORREGIDO: AlertaStockBajo es int (0 o 1)
                // ============================================================
                var alertasStock = await _context.VwAlertasStockBajos
                    .Where(a => a.AlertaStockBajo == 1 && a.Activo == true)
                    .OrderBy(a => a.StockActual)
                    .Take(10)
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
                        AlertaStockBajo = a.AlertaStockBajo,  // ✅ int
                        Activo = a.Activo
                    })
                    .ToListAsync();

                // ============================================================
                // TABLAS RECIENTES - Últimos movimientos
                // ============================================================
                var ultimosMovimientos = await _context.KardexInventarios
                    .Include(k => k.IdMaterialNavigation)
                    .Include(k => k.IdUsuarioNavigation)
                    .Include(k => k.IdTipoMovimientoNavigation)
                    .OrderByDescending(k => k.FechaMovimiento)
                    .Take(8)
                    .Select(k => new UltimoMovimientoDto
                    {
                        IdMovimiento = k.IdMovimiento,
                        IdMaterial = k.IdMaterial,
                        Material = k.IdMaterialNavigation.Nombre,
                        CodigoBarras = k.IdMaterialNavigation.CodigoBarras,
                        Cantidad = k.Cantidad,
                        TipoMovimiento = k.IdTipoMovimientoNavigation.Nombre,
                        Usuario = k.IdUsuarioNavigation.NombreCompleto,
                        Fecha = k.FechaMovimiento,
                        Proyecto = k.IdProyectoNavigation != null ? k.IdProyectoNavigation.CodigoProyecto : null,
                        DocumentoReferencia = k.DocumentoReferencia
                    })
                    .ToListAsync();

                // ============================================================
                // TABLAS RECIENTES - Proyectos recientes
                // ============================================================
                var proyectosRecientes = await _context.Proyectos
                    .Include(p => p.IdClienteNavigation)
                    .Include(p => p.IdEstadoNavigation)
                    .OrderByDescending(p => p.FechaRegistro)
                    .Take(5)
                    .Select(p => new ProyectoRecienteDto
                    {
                        IdProyecto = p.IdProyecto,
                        CodigoProyecto = p.CodigoProyecto,
                        Cliente = p.IdClienteNavigation.NombreOrazonSocial,
                        Estado = p.IdEstadoNavigation.Nombre,
                        FechaInicio = p.FechaInicio,
                        FechaEntregaEstimada = p.FechaEntregaEstimada,
                        PrecioVenta = p.PrecioVenta,
                        Moneda = p.Moneda ?? "PEN"
                    })
                    .ToListAsync();

                // ============================================================
                // USUARIOS EN LÍNEA (últimos 5 minutos)
                // ============================================================
                var usuariosActivos = await _context.AuthTokens
                    .Where(t => t.Activo && t.FechaCreacion >= hace5Minutos)
                    .Select(t => t.Usuario)
                    .Distinct()
                    .Take(5)
                    .Select(u => new UsuarioActivoDto
                    {
                        IdUsuario = u.IdUsuario,
                        NombreCompleto = u.NombreCompleto,
                        NombreUsuario = u.NombreUsuario,
                        NombreRol = u.IdRolNavigation.NombreRol,
                        UltimoAcceso = u.UltimoAcceso
                    })
                    .ToListAsync();

                // ============================================================
                // CONSTRUIR VIEW MODEL
                // ============================================================
                return new DashboardViewModel
                {
                    TotalMateriales = totalMateriales,
                    TotalProyectos = totalProyectos,
                    TotalProveedores = totalProveedores,
                    TotalClientes = totalClientes,
                    TotalContratistas = totalContratistas,
                    TotalEmpleados = totalEmpleados,
                    TotalMaquinarias = totalMaquinarias,
                    TotalUsuarios = totalUsuarios,
                    ComprasDelMes = comprasDelMes,
                    ComprasDelAnio = comprasDelAnio,
                    ProyectosActivos = proyectosActivos,
                    ProyectosCompletados = proyectosCompletados,
                    ProyectosPorEstado = proyectosPorEstado,
                    MaterialesMasUsados = materialesMasUsados,
                    ComprasPorMes = comprasPorMes,
                    AlertasStockBajo = alertasStock,
                    UltimosMovimientos = ultimosMovimientos,
                    ProyectosRecientes = proyectosRecientes,
                    UsuariosActivos = usuariosActivos,
                    NumeroAlertasStock = alertasStock.Count,
                    NumeroNotificaciones = alertasStock.Count + (usuariosActivos.Count > 0 ? 1 : 0),
                    FechaActualizacion = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetDashboardDataAsync");
                return new DashboardViewModel
                {
                    FechaActualizacion = DateTime.Now,
                    ProyectosPorEstado = new List<EstadoCountDto>(),
                    MaterialesMasUsados = new List<MaterialUsoDto>(),
                    ComprasPorMes = new List<ComprasPorMesDto>(),
                    AlertasStockBajo = new List<AlertasStockDto>(),
                    UltimosMovimientos = new List<UltimoMovimientoDto>(),
                    ProyectosRecientes = new List<ProyectoRecienteDto>(),
                    UsuariosActivos = new List<UsuarioActivoDto>()
                };
            }
        }

        // ================================================================
        // ALERTAS DE STOCK
        // ✅ CORREGIDO: AlertaStockBajo es int (0 o 1)
        // ================================================================

        public async Task<int> GetAlertasStockCountAsync()
        {
            try
            {
                return await _context.VwAlertasStockBajos
                    .CountAsync(a => a.AlertaStockBajo == 1 && a.Activo == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAlertasStockCountAsync");
                return 0;
            }
        }

        // ================================================================
        // ÚLTIMOS MOVIMIENTOS
        // ================================================================

        public async Task<List<UltimoMovimientoDto>> GetUltimosMovimientosAsync(int cantidad = 10)
        {
            try
            {
                return await _context.KardexInventarios
                    .Include(k => k.IdMaterialNavigation)
                    .Include(k => k.IdUsuarioNavigation)
                    .Include(k => k.IdTipoMovimientoNavigation)
                    .OrderByDescending(k => k.FechaMovimiento)
                    .Take(cantidad)
                    .Select(k => new UltimoMovimientoDto
                    {
                        IdMovimiento = k.IdMovimiento,
                        IdMaterial = k.IdMaterial,
                        Material = k.IdMaterialNavigation.Nombre,
                        CodigoBarras = k.IdMaterialNavigation.CodigoBarras,
                        Cantidad = k.Cantidad,
                        TipoMovimiento = k.IdTipoMovimientoNavigation.Nombre,
                        Usuario = k.IdUsuarioNavigation.NombreCompleto,
                        Fecha = k.FechaMovimiento,
                        Proyecto = k.IdProyectoNavigation != null ? k.IdProyectoNavigation.CodigoProyecto : null,
                        DocumentoReferencia = k.DocumentoReferencia
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetUltimosMovimientosAsync");
                return new List<UltimoMovimientoDto>();
            }
        }

        // ================================================================
        // PROYECTOS RECIENTES
        // ================================================================

        public async Task<List<ProyectoRecienteDto>> GetProyectosRecientesAsync(int cantidad = 5)
        {
            try
            {
                return await _context.Proyectos
                    .Include(p => p.IdClienteNavigation)
                    .Include(p => p.IdEstadoNavigation)
                    .OrderByDescending(p => p.FechaRegistro)
                    .Take(cantidad)
                    .Select(p => new ProyectoRecienteDto
                    {
                        IdProyecto = p.IdProyecto,
                        CodigoProyecto = p.CodigoProyecto,
                        Cliente = p.IdClienteNavigation.NombreOrazonSocial,
                        Estado = p.IdEstadoNavigation.Nombre,
                        FechaInicio = p.FechaInicio,
                        FechaEntregaEstimada = p.FechaEntregaEstimada,
                        PrecioVenta = p.PrecioVenta,
                        Moneda = p.Moneda ?? "PEN"
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetProyectosRecientesAsync");
                return new List<ProyectoRecienteDto>();
            }
        }

        // ================================================================
        // ESTADÍSTICAS RÁPIDAS
        // ✅ CORREGIDO: AlertaStockBajo es int (0 o 1)
        // ================================================================

        public async Task<DashboardEstadisticasDto> GetEstadisticasRapidasAsync()
        {
            try
            {
                var inicioMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                return new DashboardEstadisticasDto
                {
                    TotalMateriales = await _context.Materiales.CountAsync(m => m.Activo),
                    TotalProyectos = await _context.Proyectos.CountAsync(),
                    ProyectosActivos = await _context.Proyectos
                        .CountAsync(p => p.IdEstadoNavigation.Nombre == "En Proceso de Fabricacion" ||
                                         p.IdEstadoNavigation.Nombre == "En Proceso de Acabado" ||
                                         p.IdEstadoNavigation.Nombre == "Instalacion de Electricidad"),
                    TotalClientes = await _context.Clientes.CountAsync(c => c.Activo),
                    TotalProveedores = await _context.Proveedores.CountAsync(p => p.Activo),
                    AlertasStockBajo = await _context.VwAlertasStockBajos
                        .CountAsync(a => a.AlertaStockBajo == 1 && a.Activo == true),  // ✅ int
                    ComprasMes = await _context.Compras
                        .Where(c => c.FechaCompra >= inicioMes && c.Estado != "Anulada")
                        .SumAsync(c => c.Total),
                    UsuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo),
                    TotalContratistas = await _context.Contratistas.CountAsync(c => c.Activo),
                    TotalEmpleados = await _context.Empleados.CountAsync(e => e.Activo),
                    TotalMaquinarias = await _context.Maquinarias.CountAsync(m => m.Activo)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetEstadisticasRapidasAsync");
                return new DashboardEstadisticasDto();
            }
        }

        // ================================================================
        // USUARIOS EN LÍNEA
        // ================================================================

        public async Task<List<UsuarioActivoDto>> GetUsuariosEnLineaAsync()
        {
            try
            {
                var hace5Minutos = DateTime.Now.AddMinutes(-5);

                return await _context.AuthTokens
                    .Where(t => t.Activo && t.FechaCreacion >= hace5Minutos)
                    .Select(t => t.Usuario)
                    .Distinct()
                    .Take(10)
                    .Select(u => new UsuarioActivoDto
                    {
                        IdUsuario = u.IdUsuario,
                        NombreCompleto = u.NombreCompleto,
                        NombreUsuario = u.NombreUsuario,
                        NombreRol = u.IdRolNavigation.NombreRol,
                        UltimoAcceso = u.UltimoAcceso
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetUsuariosEnLineaAsync");
                return new List<UsuarioActivoDto>();
            }
        }

        // ================================================================
        // MÉTODOS ADICIONALES PARA COMPLETAR LA INTERFAZ
        // ================================================================

        public async Task<IEnumerable<EstadoCountDto>> GetProyectosPorEstadoAsync()
        {
            try
            {
                return await _context.Proyectos
                    .Include(p => p.IdEstadoNavigation)
                    .GroupBy(p => p.IdEstadoNavigation.Nombre)
                    .Select(g => new EstadoCountDto
                    {
                        Estado = g.Key,
                        Cantidad = g.Count()
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetProyectosPorEstadoAsync");
                return new List<EstadoCountDto>();
            }
        }

        public async Task<IEnumerable<ComprasPorMesDto>> GetComprasPorMesAsync(int meses = 12)
        {
            try
            {
                var fechaInicio = DateTime.Now.AddMonths(-meses);

                return await _context.Compras
                    .Where(c => c.FechaCompra >= fechaInicio && c.Estado != "Anulada")
                    .GroupBy(c => new { c.FechaCompra.Year, c.FechaCompra.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new ComprasPorMesDto
                    {
                        Mes = $"{g.Key.Month:00}/{g.Key.Year}",
                        MesNumero = g.Key.Month,
                        Anio = g.Key.Year,
                        Total = g.Sum(c => c.Total)
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetComprasPorMesAsync");
                return new List<ComprasPorMesDto>();
            }
        }
    }
}
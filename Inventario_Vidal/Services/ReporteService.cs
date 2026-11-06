// Services/ReporteService.cs
using ClosedXML.Excel;
using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace Inventario_Vidal.Services
{
    public class ReporteService : IReporteService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReporteService> _logger;

        private static class ColoresMarca
        {
            public const string Header = "#0a1628";
            public const string HeaderText = "#FFFFFF";
            public const string SubHeader = "#8a94a6";
            public const string TableHeader = "#0a1628";
            public const string Border = "#e8edf5";
            public const string AlternateRow = "#f9fafc";
            public const string Text = "#0a1628";
        }

        public ReporteService(
            FabricacionesVidalEirlBdContext context,
            IWebHostEnvironment env,
            ILogger<ReporteService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        private async Task<(string RazonSocial, string Ruc, string? LogoAbsolutePath)> ObtenerDatosEmpresaAsync()
        {
            var config = await _context.ConfiguracionEmpresas.FirstOrDefaultAsync();

            string? logoAbsolutePath = null;
            if (!string.IsNullOrEmpty(config?.LogoUrl))
            {
                var candidato = Path.Combine(_env.WebRootPath, config.LogoUrl.TrimStart('/', '\\'));
                if (File.Exists(candidato))
                {
                    logoAbsolutePath = candidato;
                }
            }

            return (
                config?.NombreEmpresa ?? "Fabricaciones de Estructuras Metalicas Vidal E.I.R.L.",
                config?.Ruc ?? "20612406163",
                logoAbsolutePath
            );
        }

        private string GetColumnLetter(int columnNumber)
        {
            if (columnNumber <= 0) return "A";

            string columnLetter = "";
            while (columnNumber > 0)
            {
                columnNumber--;
                columnLetter = (char)('A' + columnNumber % 26) + columnLetter;
                columnNumber /= 26;
            }
            return columnLetter;
        }

        // ================================================================
        // REPORTE - STOCK
        // ================================================================

        public async Task<IEnumerable<ReporteStockDto>> GetReporteStockAsync(bool soloAlertas = false)
        {
            try
            {
                var query = _context.Materiales
                    .Include(m => m.IdCategoriaNavigation)
                    .Include(m => m.IdUnidadConsumoNavigation)
                    .Where(m => m.Activo)
                    .AsQueryable();

                if (soloAlertas)
                {
                    query = query.Where(m => m.StockActual <= m.StockMinimo);
                }

                var materiales = await query.ToListAsync();
                var result = new List<ReporteStockDto>();

                foreach (var m in materiales)
                {
                    var ultimoCosto = await _context.MaterialProveedores
                        .Where(mp => mp.IdMaterial == m.IdMaterial)
                        .OrderByDescending(mp => mp.FechaActualizacion)
                        .Select(mp => mp.CostoUnitario)
                        .FirstOrDefaultAsync();

                    result.Add(new ReporteStockDto
                    {
                        IdMaterial = m.IdMaterial,
                        CodigoBarras = m.CodigoBarras,
                        Nombre = m.Nombre,
                        Categoria = m.IdCategoriaNavigation.Nombre,
                        Marca = m.IdMarcaNavigation != null ? m.IdMarcaNavigation.Nombre : null,
                        UnidadConsumo = m.IdUnidadConsumoNavigation.Nombre,
                        StockActual = m.StockActual,
                        StockMinimo = m.StockMinimo,
                        StockMaximo = m.StockMaximo,
                        StockReservado = m.StockReservado,
                        StockDisponible = m.StockActual - m.StockReservado,
                        Valorizado = m.StockActual * (ultimoCosto > 0 ? ultimoCosto : 0),
                        AlertaStockBajo = m.StockActual <= m.StockMinimo,
                        Activo = m.Activo
                    });
                }

                return result
                    .OrderBy(r => r.AlertaStockBajo ? 0 : 1)
                    .ThenBy(r => r.Nombre)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteStockAsync");
                return new List<ReporteStockDto>();
            }
        }

        // ================================================================
        // REPORTE - PROYECTOS
        // ================================================================

        public async Task<IEnumerable<ReporteProyectoDto>> GetReporteProyectosAsync(DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var query = _context.Proyectos
                    .Include(p => p.IdClienteNavigation)
                    .Include(p => p.IdTipoProyectoNavigation)
                    .Include(p => p.IdEstadoNavigation)
                    .Include(p => p.IdUsuarioResponsableNavigation)
                    .AsQueryable();

                if (desde.HasValue)
                    query = query.Where(p => p.FechaInicio.ToDateTime(TimeOnly.MinValue) >= desde.Value);

                if (hasta.HasValue)
                    query = query.Where(p => p.FechaInicio.ToDateTime(TimeOnly.MinValue) <= hasta.Value);

                var proyectos = await query.ToListAsync();
                var result = new List<ReporteProyectoDto>();

                foreach (var p in proyectos)
                {
                    // Incluye entregas, sobrantes y costos adicionales según SQL Server.
                    var costoReal = await ObtenerCostoTotalProyectoAsync(p.IdProyecto);

                    result.Add(new ReporteProyectoDto
                    {
                        IdProyecto = p.IdProyecto,
                        Codigo = p.CodigoProyecto,
                        Cliente = p.IdClienteNavigation.NombreOrazonSocial,
                        TipoProyecto = p.IdTipoProyectoNavigation.Nombre,
                        Estado = p.IdEstadoNavigation.Nombre,
                        FechaInicio = p.FechaInicio,
                        FechaEntregaEstimada = p.FechaEntregaEstimada,
                        FechaEntregaReal = p.FechaEntregaReal,
                        PrecioVenta = p.PrecioVenta,
                        CostoEstimado = p.CostoEstimado,
                        CostoReal = costoReal,
                        UtilidadEstimada = (p.PrecioVenta ?? 0) - p.CostoEstimado,
                        Moneda = p.Moneda ?? "PEN",
                        Responsable = p.IdUsuarioResponsableNavigation.NombreCompleto,
                        DiasTranscurridos = (DateTime.Now - p.FechaInicio.ToDateTime(TimeOnly.MinValue)).Days,
                        EstadoEntrega = p.FechaEntregaReal.HasValue ? "Entregado" : "Pendiente"
                    });
                }

                return result.OrderByDescending(r => r.FechaInicio).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteProyectosAsync");
                return new List<ReporteProyectoDto>();
            }
        }

        // ================================================================
        // REPORTE - COMPRAS
        // ================================================================

        public async Task<IEnumerable<ReporteCompraDto>> GetReporteComprasAsync(DateTime? desde = null, DateTime? hasta = null, int? idProveedor = null)
        {
            try
            {
                var query = _context.Compras
                    .Include(c => c.IdProveedorNavigation)
                    .Include(c => c.DetalleCompras)
                        .ThenInclude(d => d.IdMaterialNavigation)
                    .Where(c => c.Estado != "Anulada")
                    .AsQueryable();

                if (desde.HasValue)
                    query = query.Where(c => c.FechaCompra >= desde.Value);

                if (hasta.HasValue)
                    query = query.Where(c => c.FechaCompra <= hasta.Value);

                if (idProveedor.HasValue)
                    query = query.Where(c => c.IdProveedor == idProveedor.Value);

                var compras = await query
                    .OrderByDescending(c => c.FechaCompra)
                    .ToListAsync();

                var result = new List<ReporteCompraDto>();

                foreach (var c in compras)
                {
                    foreach (var d in c.DetalleCompras)
                    {
                        result.Add(new ReporteCompraDto
                        {
                            IdCompra = c.IdCompra,
                            NumeroComprobante = c.NumeroComprobante,
                            TipoComprobante = c.TipoComprobante,
                            Proveedor = c.IdProveedorNavigation.RazonSocial,
                            Material = d.IdMaterialNavigation.Nombre,
                            Cantidad = d.Cantidad,
                            CostoUnitario = d.CostoUnitario,
                            Subtotal = d.Subtotal ?? 0,
                            Moneda = d.Moneda ?? "PEN",
                            FechaCompra = c.FechaCompra,
                            Estado = c.Estado,
                            FechaRecepcion = c.FechaRecepcion,
                            CostoUnitarioPen = d.CostoUnitario
                        });
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteComprasAsync");
                return new List<ReporteCompraDto>();
            }
        }

        // ================================================================
        // REPORTE - KARDEX
        // ================================================================

        public async Task<IEnumerable<ReporteKardexDto>> GetReporteKardexAsync(int? idMaterial = null, DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var query = _context.KardexInventarios
                    .Include(k => k.IdMaterialNavigation)
                    .Include(k => k.IdTipoMovimientoNavigation)
                    .Include(k => k.IdUsuarioNavigation)
                    .AsQueryable();

                if (idMaterial.HasValue)
                    query = query.Where(k => k.IdMaterial == idMaterial.Value);

                if (desde.HasValue)
                    query = query.Where(k => k.FechaMovimiento >= desde.Value);

                if (hasta.HasValue)
                    query = query.Where(k => k.FechaMovimiento <= hasta.Value);

                return await query
                    .OrderBy(k => k.FechaMovimiento)
                    .Select(k => new ReporteKardexDto
                    {
                        IdMovimiento = k.IdMovimiento,
                        CodigoBarras = k.IdMaterialNavigation.CodigoBarras,
                        Material = k.IdMaterialNavigation.Nombre,
                        TipoMovimiento = k.IdTipoMovimientoNavigation.Nombre,
                        Signo = k.IdTipoMovimientoNavigation.Signo,
                        Cantidad = k.Cantidad,
                        StockAnterior = k.StockAnterior ?? 0,
                        StockPosterior = k.StockPosterior ?? 0,
                        CodigoProyecto = k.IdProyectoNavigation != null ? k.IdProyectoNavigation.CodigoProyecto : null,
                        Contratista = k.IdContratistaNavigation != null ? k.IdContratistaNavigation.NombreCompleto : null,
                        Empleado = k.IdEmpleadoNavigation != null ? k.IdEmpleadoNavigation.NombreCompleto : null,
                        Comprobante = k.DocumentoReferencia,
                        RegistradoPor = k.IdUsuarioNavigation.NombreCompleto,
                        FechaMovimiento = k.FechaMovimiento,
                        Observacion = k.Observacion
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteKardexAsync");
                return new List<ReporteKardexDto>();
            }
        }

        // ================================================================
        // REPORTE - MAQUINARIA
        // ================================================================

        public async Task<IEnumerable<ReporteMaquinariaDto>> GetReporteMaquinariaAsync()
        {
            try
            {
                var maquinarias = await _context.Maquinarias
                    .Include(m => m.IdEstadoMaquinariaNavigation)
                    .Where(m => m.Activo)
                    .ToListAsync();

                var result = new List<ReporteMaquinariaDto>();

                foreach (var m in maquinarias)
                {
                    var totalPrestamos = await _context.PrestamosMaquinaria
                        .CountAsync(p => p.IdMaquinaria == m.IdMaquinaria);

                    var totalMantenimientos = await _context.MantenimientoMaquinaria
                        .CountAsync(mt => mt.IdMaquinaria == m.IdMaquinaria);

                    var costoMantenimiento = await _context.MantenimientoMaquinaria
                        .Where(mt => mt.IdMaquinaria == m.IdMaquinaria)
                        .SumAsync(mt => mt.Costo);

                    result.Add(new ReporteMaquinariaDto
                    {
                        IdMaquinaria = m.IdMaquinaria,
                        Codigo = m.Codigo,
                        Nombre = m.Nombre,
                        Marca = m.Marca ?? "No registrada",
                        Modelo = m.Modelo ?? "No registrado",
                        Estado = m.IdEstadoMaquinariaNavigation.Nombre,
                        TotalPrestamos = totalPrestamos,
                        TotalMantenimientos = totalMantenimientos,
                        CostoMantenimiento = costoMantenimiento,
                        Moneda = m.Moneda ?? "PEN"
                    });
                }

                return result.OrderBy(r => r.Nombre).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteMaquinariaAsync");
                return new List<ReporteMaquinariaDto>();
            }
        }

        // ================================================================
        // REPORTE - CLIENTES
        // ================================================================

        public async Task<IEnumerable<ReporteClienteDto>> GetReporteClientesAsync(string search = "", string tipo = "", string orden = "proyectos")
        {
            try
            {
                var query = _context.Clientes
                    .Include(c => c.Proyectos)
                        .ThenInclude(p => p.IdEstadoNavigation)
                    .Where(c => c.Activo);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(c =>
                        c.NumeroDocumento.ToLower().Contains(search) ||
                        c.NombreOrazonSocial.ToLower().Contains(search) ||
                        (c.Telefono != null && c.Telefono.Contains(search)));
                }

                if (!string.IsNullOrEmpty(tipo))
                {
                    query = query.Where(c => c.TipoCliente == tipo);
                }

                var clientes = await query.ToListAsync();

                var result = clientes.Select(c => new ReporteClienteDto
                {
                    IdCliente = c.IdCliente,
                    NumeroDocumento = c.NumeroDocumento,
                    NombreOrazonSocial = c.NombreOrazonSocial,
                    TipoCliente = c.TipoCliente,
                    TipoDocumento = c.TipoDocumento,
                    Telefono = c.Telefono ?? "-",
                    CorreoElectronico = c.CorreoElectronico ?? "-",
                    Direccion = c.Direccion,
                    Distrito = c.Distrito,
                    Provincia = c.Provincia,
                    Departamento = c.Departamento,
                    TotalProyectos = c.Proyectos?.Count(p => p.IdEstadoNavigation != null && p.IdEstadoNavigation.Nombre != "Cancelado") ?? 0,
                    TotalInvertido = c.Proyectos?.Sum(p => p.PrecioVenta ?? 0) ?? 0,
                    FechaRegistro = c.FechaRegistro,
                    Activo = c.Activo
                }).ToList();

                result = orden switch
                {
                    "invertido" => result.OrderByDescending(x => x.TotalInvertido).ToList(),
                    "nombre" => result.OrderBy(x => x.NombreOrazonSocial).ToList(),
                    _ => result.OrderByDescending(x => x.TotalProyectos).ToList()
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteClientesAsync");
                return new List<ReporteClienteDto>();
            }
        }

        // ================================================================
        // REPORTE - CONTRATISTAS
        // ================================================================

        public async Task<IEnumerable<ReporteContratistaDto>> GetReporteContratistasAsync(string search = "", string especialidad = "", string orden = "horas")
        {
            try
            {
                var query = _context.Contratistas
                    .Where(c => c.Activo);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(c =>
                        c.NumeroDocumento.ToLower().Contains(search) ||
                        c.NombreCompleto.ToLower().Contains(search) ||
                        (c.Telefono != null && c.Telefono.Contains(search)));
                }

                if (!string.IsNullOrEmpty(especialidad))
                {
                    query = query.Where(c => c.Especialidad == especialidad);
                }

                var contratistas = await query.ToListAsync();
                var result = new List<ReporteContratistaDto>();

                foreach (var c in contratistas)
                {
                    // Las entregas de material no acreditan horas ni pagos de contratistas.
                    var recursosAsignados = await _context.AsignacionMaterialesPersonal
                        .CountAsync(a => a.IdContratista == c.IdContratista);

                    var proyectosAsignados = await _context.AsignacionPersonalProyectos
                        .CountAsync(a => a.IdContratista == c.IdContratista);

                    result.Add(new ReporteContratistaDto
                    {
                        IdContratista = c.IdContratista,
                        NumeroDocumento = c.NumeroDocumento,
                        NombreCompleto = c.NombreCompleto,
                        Especialidad = c.Especialidad ?? "General",
                        Telefono = c.Telefono ?? "-",
                        CorreoElectronico = c.CorreoElectronico ?? "-",
                        TotalProyectosAsignados = proyectosAsignados,
                        TotalRecursosAsignados = recursosAsignados,
                        TotalHoras = 0,
                        TarifaHora = 0m,
                        TotalPagado = 0m,
                        TipoDocumento = c.TipoDocumento,
                        FechaRegistro = c.FechaRegistro,
                        Activo = c.Activo,
                        Calificacion = c.Calificacion
                    });
                }

                result = orden switch
                {
                    "pagado" => result.OrderByDescending(x => x.TotalPagado).ToList(),
                    "nombre" => result.OrderBy(x => x.NombreCompleto).ToList(),
                    _ => result.OrderByDescending(x => x.TotalHoras).ToList()
                };

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteContratistasAsync");
                return new List<ReporteContratistaDto>();
            }
        }

        // ================================================================
        // REPORTE - UTILIDAD
        // ================================================================

        public async Task<IEnumerable<ReporteUtilidadDto>> GetReporteUtilidadAsync(string search = "", string resultado = "", DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var query = _context.Proyectos
                    .Include(p => p.IdClienteNavigation)
                    .Include(p => p.CostosProyectos)
                    .Include(p => p.IdEstadoNavigation)
                    .Where(p => p.IdEstadoNavigation.Nombre != "Cancelado");

                if (desde.HasValue)
                    query = query.Where(p => p.FechaInicio >= DateOnly.FromDateTime(desde.Value));
                if (hasta.HasValue)
                    query = query.Where(p => p.FechaInicio <= DateOnly.FromDateTime(hasta.Value));

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim().ToLower();
                    query = query.Where(p =>
                        p.CodigoProyecto.ToLower().Contains(search) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(search)));
                }

                var proyectos = await query.ToListAsync();

                var result = new List<ReporteUtilidadDto>();
                foreach (var p in proyectos)
                {
                    var costos = await ObtenerCostoTotalProyectoAsync(p.IdProyecto);
                    var ingresos = p.PrecioVenta ?? 0;
                    var utilidad = ingresos - costos;
                    var margen = ingresos > 0 ? (utilidad / ingresos) * 100 : 0;

                    result.Add(new ReporteUtilidadDto
                    {
                        IdProyecto = p.IdProyecto,
                        Codigo = p.CodigoProyecto,
                        Nombre = p.Descripcion ?? p.CodigoProyecto,
                        Cliente = p.IdClienteNavigation?.NombreOrazonSocial ?? "N/A",
                        FechaInicio = p.FechaInicio,
                        FechaEntregaReal = p.FechaEntregaReal,
                        Ingresos = ingresos,
                        Costos = costos,
                        Utilidad = utilidad,
                        Margen = Math.Round(margen, 1),
                        Moneda = p.Moneda ?? "PEN",
                        Estado = p.IdEstadoNavigation?.Nombre ?? "Sin Estado"
                    });
                }

                if (resultado == "ganancia")
                    result = result.Where(r => r.Utilidad >= 0).ToList();
                else if (resultado == "perdida")
                    result = result.Where(r => r.Utilidad < 0).ToList();

                return result.OrderByDescending(r => r.Utilidad).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteUtilidadAsync");
                return new List<ReporteUtilidadDto>();
            }
        }

        // ================================================================
        // ESTADÍSTICAS RÁPIDAS
        // ================================================================

        public async Task<EstadisticasRapidasDto> GetEstadisticasRapidasAsync()
        {
            try
            {
                var materiales = await _context.Materiales.CountAsync(m => m.Activo);
                var proyectos = await _context.Proyectos.CountAsync(p => p.IdEstadoNavigation.Nombre != "Cancelado");
                var compras = await _context.Compras.CountAsync(c => c.Estado == "Registrada");
                var alertas = await _context.VwAlertasStockBajos.CountAsync();
                var clientes = await _context.Clientes.CountAsync(c => c.Activo);
                var contratistas = await _context.Contratistas.CountAsync(c => c.Activo);
                var maquinarias = await _context.Maquinarias.CountAsync(m => m.Activo);

                var proyectosParaUtilidad = await _context.Proyectos
                    .AsNoTracking()
                    .Where(p => p.IdEstadoNavigation.Nombre != "Cancelado")
                    .Select(p => new { p.IdProyecto, p.PrecioVenta })
                    .ToListAsync();
                decimal utilidadTotal = 0m;
                foreach (var p in proyectosParaUtilidad)
                    utilidadTotal += (p.PrecioVenta ?? 0m) - await ObtenerCostoTotalProyectoAsync(p.IdProyecto);

                var totalComprasValor = await _context.Compras
                    .Where(c => c.Estado != "Anulada")
                    .SumAsync(c => c.Total);

                var proyectosActivos = await _context.Proyectos
                    .CountAsync(p => p.IdEstadoNavigation.Nombre == "En Proceso de Fabricacion" ||
                                     p.IdEstadoNavigation.Nombre == "En Proceso de Acabado" ||
                                     p.IdEstadoNavigation.Nombre == "Instalacion de Electricidad");

                var proyectosEntregados = await _context.Proyectos
                    .CountAsync(p => p.IdEstadoNavigation.Nombre == "Entregado");

                return new EstadisticasRapidasDto
                {
                    TotalMateriales = materiales,
                    TotalProyectos = proyectos,
                    TotalCompras = compras,
                    AlertasStock = alertas,
                    TotalClientes = clientes,
                    TotalContratistas = contratistas,
                    TotalMaquinarias = maquinarias,
                    UtilidadTotal = utilidadTotal,
                    TotalComprasValor = totalComprasValor,
                    ProyectosActivos = proyectosActivos,
                    ProyectosEntregados = proyectosEntregados,
                    Moneda = "PEN"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetEstadisticasRapidasAsync");
                return new EstadisticasRapidasDto();
            }
        }

        // ================================================================
        // EXPORTAR EXCEL
        // ================================================================

        public async Task<byte[]> ExportarExcelAsync<T>(IEnumerable<T> data, string titulo, string[]? columnas = null)
        {
            try
            {
                var (razonSocial, ruc, logoPath) = await ObtenerDatosEmpresaAsync();
                var properties = typeof(T).GetProperties();

                var encabezados = (columnas != null && columnas.Length == properties.Length)
                    ? columnas
                    : properties.Select(p => p.Name).ToArray();

                var filas = data.ToList();
                var totalColumnas = properties.Length;

                using var workbook = new XLWorkbook();
                var hoja = workbook.Worksheets.Add(titulo.Length > 31 ? titulo[..31] : titulo);

                var ultimaColumnaLetra = GetColumnLetter(totalColumnas + 1);

                // Encabezado con logo
                for (int fila = 1; fila <= 4; fila++)
                {
                    var rangoFondo = hoja.Range($"A{fila}:{ultimaColumnaLetra}{fila}");
                    rangoFondo.Style.Fill.BackgroundColor = XLColor.FromHtml(ColoresMarca.Header);

                    var rangoTexto = hoja.Range($"B{fila}:{ultimaColumnaLetra}{fila}");
                    rangoTexto.Merge();
                }

                hoja.Row(1).Height = 25;
                hoja.Row(2).Height = 18;
                hoja.Row(3).Height = 30;
                hoja.Row(4).Height = 20;

                if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                {
                    using var logoStream = File.OpenRead(logoPath);
                    hoja.AddPicture(logoStream)
                        .MoveTo(hoja.Cell("A1"), 8, 8)
                        .WithSize(65, 65);
                }

                hoja.Cell("B1").Value = razonSocial;
                hoja.Cell("B1").Style.Font.FontColor = XLColor.White;
                hoja.Cell("B1").Style.Font.Bold = true;
                hoja.Cell("B1").Style.Font.FontSize = 14;

                hoja.Cell("B2").Value = $"RUC: {ruc}";
                hoja.Cell("B2").Style.Font.FontColor = XLColor.FromHtml(ColoresMarca.SubHeader);
                hoja.Cell("B2").Style.Font.FontSize = 10;

                hoja.Cell("B3").Value = titulo;
                hoja.Cell("B3").Style.Font.FontColor = XLColor.White;
                hoja.Cell("B3").Style.Font.Bold = true;
                hoja.Cell("B3").Style.Font.FontSize = 18;

                hoja.Cell("B4").Value = $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}";
                hoja.Cell("B4").Style.Font.FontColor = XLColor.FromHtml(ColoresMarca.SubHeader);
                hoja.Cell("B4").Style.Font.FontSize = 10;

                const int filaEncabezadoTabla = 6;
                for (int i = 0; i < properties.Length; i++)
                {
                    var celda = hoja.Cell(filaEncabezadoTabla, i + 1);
                    celda.Value = encabezados[i];
                    celda.Style.Fill.BackgroundColor = XLColor.FromHtml(ColoresMarca.TableHeader);
                    celda.Style.Font.FontColor = XLColor.White;
                    celda.Style.Font.Bold = true;
                    celda.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }
                hoja.Row(filaEncabezadoTabla).Height = 22;

                int filaActual = filaEncabezadoTabla + 1;
                int rowIndex = 0;

                foreach (var item in filas)
                {
                    for (int i = 0; i < properties.Length; i++)
                    {
                        var valor = properties[i].GetValue(item);
                        var celda = hoja.Cell(filaActual, i + 1);

                        if (valor is DateTime fechaValor)
                        {
                            celda.Value = fechaValor;
                            celda.Style.DateFormat.Format = "dd/MM/yyyy";
                        }
                        else if (valor is decimal decValor)
                        {
                            celda.Value = (double)decValor;
                            celda.Style.NumberFormat.Format = "#,##0.00";
                        }
                        else if (valor is double dblValor)
                        {
                            celda.Value = dblValor;
                            celda.Style.NumberFormat.Format = "#,##0.00";
                        }
                        else if (valor is int intValor)
                        {
                            celda.Value = intValor;
                        }
                        else if (valor is long longValor)
                        {
                            celda.Value = longValor;
                        }
                        else
                        {
                            celda.Value = valor?.ToString() ?? "";
                        }

                        celda.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        celda.Style.Font.FontColor = XLColor.FromHtml(ColoresMarca.Text);

                        if (rowIndex % 2 == 1)
                        {
                            celda.Style.Fill.BackgroundColor = XLColor.FromHtml(ColoresMarca.AlternateRow);
                        }
                    }
                    filaActual++;
                    rowIndex++;
                }

                hoja.Columns(1, Math.Max(totalColumnas, 1)).AdjustToContents();

                const double anchoMinimoLogo = 9.0;
                if (hoja.Column(1).Width < anchoMinimoLogo)
                {
                    hoja.Column(1).Width = anchoMinimoLogo;
                }

                hoja.SheetView.FreezeRows(filaEncabezadoTabla);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return await Task.FromResult(stream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ExportarExcelAsync");
                return Array.Empty<byte>();
            }
        }

        // ================================================================
        // MATERIALES MÁS UTILIZADOS
        // ================================================================

        public async Task<IEnumerable<MaterialUsoDto>> GetMaterialesMasUtilizadosAsync(int cantidad = 10, DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var query = _context.KardexInventarios
                    .Include(k => k.IdMaterialNavigation)
                    .Include(k => k.IdTipoMovimientoNavigation)
                    .Where(k => k.IdTipoMovimientoNavigation.Nombre == "Salida por Asignacion a Proyecto");

                if (desde.HasValue)
                    query = query.Where(k => k.FechaMovimiento >= desde.Value);

                if (hasta.HasValue)
                    query = query.Where(k => k.FechaMovimiento <= hasta.Value);

                var totalUso = await query.SumAsync(k => k.Cantidad);

                return await query
                    .GroupBy(k => new { k.IdMaterial, k.IdMaterialNavigation.Nombre })
                    .Select(g => new MaterialUsoDto
                    {
                        IdMaterial = g.Key.IdMaterial,
                        Material = g.Key.Nombre,
                        Cantidad = g.Sum(k => k.Cantidad),
                        Porcentaje = totalUso > 0 ? Math.Round((g.Sum(k => k.Cantidad) / totalUso) * 100, 1) : 0
                    })
                    .OrderByDescending(m => m.Cantidad)
                    .Take(cantidad)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetMaterialesMasUtilizadosAsync");
                return new List<MaterialUsoDto>();
            }
        }

        // ================================================================
        // REPORTE - CONSUMO POR PROYECTO
        // ================================================================

        public async Task<IEnumerable<ReporteConsumoProyectoDto>> GetReporteConsumoProyectoAsync(int idProyecto)
        {
            try
            {
                var proyecto = await _context.Proyectos
                    .FirstOrDefaultAsync(p => p.IdProyecto == idProyecto);

                if (proyecto == null)
                {
                    return new List<ReporteConsumoProyectoDto>();
                }

                // ✅ Usar AsignacionMaterialesPersonal
                return await _context.AsignacionMaterialesPersonal
                    .Include(a => a.IdMaterialNavigation)
                        .ThenInclude(m => m.IdCategoriaNavigation)
                    .Include(a => a.IdMaterialNavigation)
                        .ThenInclude(m => m.IdUnidadConsumoNavigation)
                    .Where(a => a.IdProyecto == idProyecto)
                    .Select(a => new ReporteConsumoProyectoDto
                    {
                        IdMaterial = a.IdMaterial,
                        Material = a.IdMaterialNavigation.Nombre,
                        CodigoBarras = a.IdMaterialNavigation.CodigoBarras,
                        CategoriaMaterial = a.IdMaterialNavigation.IdCategoriaNavigation.Nombre,
                        UnidadConsumo = a.IdMaterialNavigation.IdUnidadConsumoNavigation.Nombre,
                        CantidadAsignada = a.CantidadEntregada,
                        // Campos heredados del DTO: ya no hay rendición por persona.
                        CantidadUtilizada = 0m,
                        CantidadDevuelta = 0m,
                        CostoUnitarioRef = a.CostoUnitarioRef,
                        FechaAsignacion = a.FechaAsignacion,
                        IdProyecto = a.IdProyecto,
                        CodigoProyecto = proyecto.CodigoProyecto
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetReporteConsumoProyectoAsync para proyecto {IdProyecto}", idProyecto);
                return new List<ReporteConsumoProyectoDto>();
            }
        }
        private async Task<decimal> ObtenerCostoTotalProyectoAsync(int idProyecto)
        {
            return await _context.Database
                .SqlQueryRaw<decimal>("SELECT dbo.fn_CostoTotalProyecto({0}) AS Value", idProyecto)
                .SingleAsync();
        }

    }
}
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    [RolRequerido("Administrador", "Jefe de Almacén", "Ingeniero")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeService _homeService;
        private readonly IAlertaService _alertaService;
        private readonly Data.FabricacionesVidalEirlBdContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            IHomeService homeService,
            IAlertaService alertaService,
            Data.FabricacionesVidalEirlBdContext context)
        {
            _logger = logger;
            _homeService = homeService;
            _alertaService = alertaService;
            _context = context;
        }

        // ================================================================
        // INDEX - DASHBOARD
        // ================================================================

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            try
            {
                var model = await _homeService.GetDashboardDataAsync();
                ViewBag.ResumenComprasBI = await ObtenerResumenComprasBiAsync(desde, hasta);

                ViewBag.NombreCompleto = HttpContext.Session.GetString("NombreCompleto") ?? "Usuario";
                ViewBag.NombreRol = HttpContext.Session.GetString("NombreRol") ?? "Sin rol";

                var resumenAlertas = await _alertaService.GetResumenAlertasAsync();
                ViewBag.TotalAlertas = resumenAlertas?.TotalAlertas ?? 0;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el Dashboard");
                // El error del BI no debe ocultarse tras un total ficticio.
                ViewBag.ErrorComprasBI = "No se pudo cargar el resumen de compras.";
                var emptyModel = new DashboardViewModel
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

                ViewBag.NombreCompleto = HttpContext.Session.GetString("NombreCompleto") ?? "Usuario";
                ViewBag.NombreRol = HttpContext.Session.GetString("NombreRol") ?? "Sin rol";
                ViewBag.TotalAlertas = 0;
                TempData["MensajeError"] = "Error al cargar los datos del dashboard. Intente nuevamente.";
                return View(emptyModel);
            }
        }

        // ================================================================
        // DASHBOARD BI: TOTALES POR MONEDA Y CONSOLIDADO EN SOLES
        // Usa exclusivamente la fecha de la compra y el TC guardado para
        // esa fecha. No usa tasas actuales, fallbacks ni estimaciones.
        // ================================================================
        private async Task<ResumenComprasBiDto> ObtenerResumenComprasBiAsync(DateTime? desde, DateTime? hasta)
        {
            var inicio = (desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var fin = (hasta ?? DateTime.Today).Date;
            if (fin < inicio || (fin - inicio).TotalDays > 3660)
                throw new ArgumentException("El rango de fechas del BI no es válido.");
            var limiteExclusivo = fin.AddDays(1);

            var compras = await _context.Compras.AsNoTracking()
                .Where(c => c.FechaCompra >= inicio && c.FechaCompra < limiteExclusivo && c.Estado != "Anulada")
                .Select(c => new { c.FechaCompra, c.Moneda, c.Total })
                .ToListAsync();
            var tcRegistrados = await _context.TiposCambios.AsNoTracking()
                .Where(t => t.Fecha >= DateOnly.FromDateTime(inicio)
                    && t.Fecha <= DateOnly.FromDateTime(fin)
                    && t.MonedaOrigen == "USD" && t.MonedaDestino == "PEN")
                .Select(t => new { t.Fecha, t.Valor })
                .ToListAsync();
            var cambios = tcRegistrados.GroupBy(t => t.Fecha)
                .ToDictionary(g => g.Key, g => g.First().Valor);

            var r = new ResumenComprasBiDto { Desde = inicio, Hasta = fin, CantidadComprobantes = compras.Count };
            var fechasFaltantes = new HashSet<string>();
            foreach (var compra in compras)
            {
                var moneda = (compra.Moneda ?? string.Empty).Trim().ToUpperInvariant();
                if (moneda is "PEN" or "PENES" or "SOLES" or "SOL" or "S/")
                {
                    r.TotalPEN += compra.Total;
                }
                else if (moneda is "USD" or "US$" or "DOLARES" or "DÓLARES")
                {
                    r.TotalUSD += compra.Total;
                    var fecha = DateOnly.FromDateTime(compra.FechaCompra);
                    if (cambios.TryGetValue(fecha, out var cambio) && cambio > 0)
                        r.TotalUSDenPEN += Math.Round(compra.Total * cambio, 2, MidpointRounding.AwayFromZero);
                    else
                    {
                        r.ComprobantesSinTipoCambio++;
                        fechasFaltantes.Add(fecha.ToString("dd/MM/yyyy"));
                    }
                }
                else r.MonedasNoReconocidas++;
            }
            r.FechasSinTipoCambio = fechasFaltantes.OrderBy(x => x).ToList();
            if (r.ComprobantesSinTipoCambio == 0 && r.MonedasNoReconocidas == 0)
                r.TotalConsolidadoPEN = r.TotalPEN + r.TotalUSDenPEN;
            return r;
        }

        // ================================================================
        // ACCESO DENEGADO
        // ================================================================

        public IActionResult AccessDenied()
        {
            return View();
        }

        // ================================================================
        // PRIVACY
        // ================================================================

        public IActionResult Privacy()
        {
            return View();
        }

        // ================================================================
        // OBTENER ESTADÍSTICAS RÁPIDAS (AJAX)
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> GetEstadisticasRapidas()
        {
            try
            {
                var estadisticas = await _homeService.GetEstadisticasRapidasAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        estadisticas.TotalMateriales,
                        estadisticas.TotalProyectos,
                        estadisticas.ProyectosActivos,
                        estadisticas.TotalClientes,
                        estadisticas.TotalProveedores,
                        estadisticas.AlertasStockBajo,
                        estadisticas.ComprasMes,
                        estadisticas.UsuariosActivos,
                        estadisticas.TotalContratistas,
                        estadisticas.TotalEmpleados,
                        estadisticas.TotalMaquinarias
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas rápidas");
                return Json(new { success = false, message = "Error al obtener estadísticas" });
            }
        }

        // ================================================================
        // OBTENER ÚLTIMOS MOVIMIENTOS (AJAX)
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> GetUltimosMovimientos(int cantidad = 8)
        {
            try
            {
                var movimientos = await _homeService.GetUltimosMovimientosAsync(cantidad);

                return Json(new
                {
                    success = true,
                    data = movimientos.Select(m => new
                    {
                        m.IdMovimiento,
                        m.Material,
                        m.CodigoBarras,
                        m.Cantidad,
                        m.TipoMovimiento,
                        m.Usuario,
                        m.Fecha,
                        m.FechaFormateada,
                        m.Proyecto,
                        m.DocumentoReferencia,
                        m.TipoBadge
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener últimos movimientos");
                return Json(new { success = false, message = "Error al obtener movimientos" });
            }
        }

        // ================================================================
        // OBTENER PROYECTOS RECIENTES (AJAX)
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> GetProyectosRecientes(int cantidad = 5)
        {
            try
            {
                var proyectos = await _homeService.GetProyectosRecientesAsync(cantidad);

                return Json(new
                {
                    success = true,
                    data = proyectos.Select(p => new
                    {
                        p.IdProyecto,
                        p.CodigoProyecto,
                        p.Cliente,
                        p.Estado,
                        p.EstadoBadge,
                        p.FechaInicio,
                        p.FechaEntregaEstimada,
                        p.PrecioVenta,
                        p.Moneda
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos recientes");
                return Json(new { success = false, message = "Error al obtener proyectos" });
            }
        }

        // ================================================================
        // OBTENER USUARIOS EN LÍNEA (AJAX)
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> GetUsuariosEnLinea()
        {
            try
            {
                var context = HttpContext.RequestServices.GetRequiredService<Data.FabricacionesVidalEirlBdContext>();
                var hace5Minutos = DateTime.Now.AddMinutes(-5);

                var usuarios = await context.AuthTokens
                    .Where(t => t.Activo && t.FechaCreacion >= hace5Minutos)
                    .Select(t => t.Usuario)
                    .Distinct()
                    .OrderByDescending(u => u.UltimoAcceso)
                    .Take(10)
                    .Select(u => new
                    {
                        u.IdUsuario,
                        u.NombreCompleto,
                        u.NombreUsuario,
                        NombreRol = u.IdRolNavigation.NombreRol,
                        u.UltimoAcceso,
                        EstaEnLinea = true,
                        Iniciales = ObtenerIniciales(u.NombreCompleto)
                    })
                    .ToListAsync();

                return Json(new { success = true, data = usuarios });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios en línea");
                return Json(new { success = false, message = "Error al obtener usuarios" });
            }
        }

        // ================================================================
        // OBTENER DATOS DEL DASHBOARD COMPLETO (AJAX)
        // ================================================================

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                var model = await _homeService.GetDashboardDataAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        model.TotalMateriales,
                        model.TotalProyectos,
                        model.TotalProveedores,
                        model.TotalClientes,
                        model.TotalContratistas,
                        model.TotalEmpleados,
                        model.TotalMaquinarias,
                        model.TotalUsuarios,
                        model.ComprasDelMes,
                        model.ComprasDelAnio,
                        model.ProyectosActivos,
                        model.ProyectosCompletados,
                        model.NumeroAlertasStock,
                        model.NumeroNotificaciones,
                        ProyectosPorEstado = model.ProyectosPorEstado.Select(p => new { p.Estado, p.Cantidad, p.Color }),
                        MaterialesMasUsados = model.MaterialesMasUsados.Select(m => new { m.Material, m.Cantidad, m.Unidad, m.Porcentaje }),
                        ComprasPorMes = model.ComprasPorMes.Select(c => new { c.Mes, c.Total }),
                        AlertasStockBajo = model.AlertasStockBajo.Select(a => new
                        {
                            a.IdMaterial,
                            a.Nombre,
                            a.CodigoBarras,
                            a.Categoria,
                            a.StockActual,
                            a.StockMinimo,
                            a.PorcentajeAlerta,
                            a.NivelAlerta,
                            a.ImagenUrl
                        }),
                        model.FechaActualizacion
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener datos del dashboard");
                return Json(new { success = false, message = "Error al obtener datos" });
            }
        }

        // ================================================================
        // MÉTODOS PRIVADOS
        // ================================================================

        private string ObtenerIniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "U";
            var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0].Substring(0, 1).ToUpper();
            return (partes[0].Substring(0, 1) + partes[^1].Substring(0, 1)).ToUpper();
        }
    }
}
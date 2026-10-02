using Inventario_Vidal.Data;
using Inventario_Vidal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Inventario_Vidal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Dashboard con KPIs
            var viewModel = new DashboardViewModel
            {
                // Inventario
                TotalMateriales = await _context.Materiales.CountAsync(m => m.Activo),
                MaterialesStockBajo = await _context.Materiales
                    .CountAsync(m => m.Activo && m.StockActual <= m.StockMinimo && m.StockMinimo > 0),
                MaterialesSinStock = await _context.Materiales
                    .CountAsync(m => m.Activo && m.StockActual == 0),

                // Proyectos
                TotalProyectos = await _context.Proyectos.CountAsync(),
                ProyectosActivos = await _context.Proyectos
                    .CountAsync(p => p.IdEstadoNavigation.Orden < 5),
                ProyectosAtrasados = await _context.Proyectos
                    .CountAsync(p => p.IdEstadoNavigation.Orden < 5
                        && p.FechaEntregaEstimada.HasValue
                        && p.FechaEntregaEstimada < DateOnly.FromDateTime(DateTime.Now)),

                // Compras
                TotalCompras = await _context.Compras.CountAsync(),
                ComprasUltimoMes = await _context.Compras
                    .Where(c => c.FechaCompra >= DateTime.Now.AddMonths(-1))
                    .SumAsync(c => c.Total),

                // Personal
                TotalClientes = await _context.Clientes.CountAsync(c => c.Activo),
                TotalProveedores = await _context.Proveedores.CountAsync(p => p.Activo),
                TotalEmpleados = await _context.Empleados.CountAsync(e => e.Activo),
                TotalContratistas = await _context.Contratistas.CountAsync(c => c.Activo),

                // Maquinaria
                TotalMaquinaria = await _context.Maquinarias.CountAsync(m => m.Activo),
                MaquinariaDisponible = await _context.Maquinarias
                    .CountAsync(m => m.Activo && m.IdEstadoMaquinariaNavigation.Nombre == "Disponible"),

                // Últimos movimientos de Kardex
                UltimosMovimientos = await _context.KardexInventarios
                    .Include(k => k.IdMaterialNavigation)
                    .Include(k => k.IdTipoMovimientoNavigation)
                    .Include(k => k.IdUsuarioNavigation)
                    .OrderByDescending(k => k.FechaMovimiento)
                    .Take(5)
                    .ToListAsync(),

                // Materiales con stock bajo
                MaterialesAlerta = await _context.Materiales
                    .Where(m => m.Activo && m.StockActual <= m.StockMinimo && m.StockMinimo > 0)
                    .OrderBy(m => m.StockActual)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // ViewModel para el Dashboard
    public class DashboardViewModel
    {
        public int TotalMateriales { get; set; }
        public int MaterialesStockBajo { get; set; }
        public int MaterialesSinStock { get; set; }

        public int TotalProyectos { get; set; }
        public int ProyectosActivos { get; set; }
        public int ProyectosAtrasados { get; set; }

        public int TotalCompras { get; set; }
        public decimal ComprasUltimoMes { get; set; }

        public int TotalClientes { get; set; }
        public int TotalProveedores { get; set; }
        public int TotalEmpleados { get; set; }
        public int TotalContratistas { get; set; }

        public int TotalMaquinaria { get; set; }
        public int MaquinariaDisponible { get; set; }

        public List<KardexInventario> UltimosMovimientos { get; set; } = new();
        public List<Materiale> MaterialesAlerta { get; set; } = new();
    }
}
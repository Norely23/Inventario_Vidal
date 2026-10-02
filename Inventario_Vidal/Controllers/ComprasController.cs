using Inventario_Vidal.Data;
using Inventario_Vidal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    public class ComprasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComprasController> _logger;

        public ComprasController(ApplicationDbContext context, ILogger<ComprasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Compras
        public async Task<IActionResult> Index(string buscar, int? proveedorId, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Compras
                .Include(c => c.IdProveedorNavigation)
                .Include(c => c.IdUsuarioNavigation)
                .Include(c => c.DetalleCompras)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(c =>
                    c.NumeroComprobante.Contains(buscar) ||
                    c.IdProveedorNavigation.RazonSocial.Contains(buscar));
            }

            if (proveedorId.HasValue && proveedorId.Value > 0)
                query = query.Where(c => c.IdProveedor == proveedorId.Value);

            if (desde.HasValue)
                query = query.Where(c => c.FechaCompra >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(c => c.FechaCompra <= hasta.Value.AddDays(1));

            ViewData["Buscar"] = buscar;
            ViewData["ProveedorId"] = proveedorId;
            ViewData["Desde"] = desde?.ToString("yyyy-MM-dd");
            ViewData["Hasta"] = hasta?.ToString("yyyy-MM-dd");
            ViewData["Proveedores"] = new SelectList(
                await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync(),
                "IdProveedor", "RazonSocial", proveedorId);

            var compras = await query.OrderByDescending(c => c.FechaCompra).ToListAsync();
            return View(compras);
        }

        // GET: Compras/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var compra = await _context.Compras
                .Include(c => c.IdProveedorNavigation)
                .Include(c => c.IdUsuarioNavigation)
                .Include(c => c.DetalleCompras)
                    .ThenInclude(d => d.IdMaterialNavigation)
                .Include(c => c.DetalleCompras)
                    .ThenInclude(d => d.IdPresentacionNavigation)
                .FirstOrDefaultAsync(m => m.IdCompra == id);

            if (compra == null) return NotFound();

            return View(compra);
        }

        // GET: Compras/Create
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View();
        }

        // POST: Compras/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("NumeroComprobante,TipoComprobante,IdProveedor,Moneda,Observaciones")] Compra compra,
            List<int>? MaterialIds,
            List<decimal>? Cantidades,
            List<decimal>? Costos,
            List<string>? PresentacionIdsStr,
            List<string>? UnidadCompraIdsStr)
        {
            // 🔍 DIAGNÓSTICO: Ver qué llegó al servidor
            _logger.LogInformation("=== POST Compras/Create INICIADO ===");
            _logger.LogInformation($"ProveedorId: {compra.IdProveedor}");
            _logger.LogInformation($"NumeroComprobante: {compra.NumeroComprobante}");
            _logger.LogInformation($"TipoComprobante: {compra.TipoComprobante}");
            _logger.LogInformation($"MaterialIds count: {MaterialIds?.Count ?? 0}");
            _logger.LogInformation($"Cantidades count: {Cantidades?.Count ?? 0}");
            _logger.LogInformation($"Costos count: {Costos?.Count ?? 0}");
            _logger.LogInformation($"ModelState.IsValid: {ModelState.IsValid}");

            // Mostrar errores de ModelState
            foreach (var key in ModelState.Keys)
            {
                var errors = ModelState[key].Errors;
                foreach (var error in errors)
                {
                    _logger.LogWarning($"ModelState ERROR en '{key}': {error.ErrorMessage}");
                }
            }

            // Validaciones manuales
            if (compra.IdProveedor == 0)
                ModelState.AddModelError("IdProveedor", "Debe seleccionar un proveedor.");

            if (string.IsNullOrWhiteSpace(compra.NumeroComprobante))
                ModelState.AddModelError("NumeroComprobante", "El número de comprobante es obligatorio.");

            if (MaterialIds == null || !MaterialIds.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos un material.");
                _logger.LogWarning("No llegaron MaterialIds del formulario.");
            }

            // Validar comprobante único
            if (compra.IdProveedor > 0 && !string.IsNullOrWhiteSpace(compra.NumeroComprobante))
            {
                bool existe = await _context.Compras.AnyAsync(c =>
                    c.IdProveedor == compra.IdProveedor &&
                    c.NumeroComprobante == compra.NumeroComprobante);

                if (existe)
                    ModelState.AddModelError("NumeroComprobante", "Ya existe una compra con este comprobante para este proveedor.");
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Obtener el primer usuario activo
                    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Activo);
                    if (usuario == null)
                        throw new Exception("No hay usuarios activos en el sistema. Ejecute el script SQL para insertar un usuario admin.");

                    // Crear la compra
                    compra.FechaCompra = DateTime.Now;
                    compra.Estado = "Registrada";
                    compra.IdUsuario = usuario.IdUsuario;
                    compra.Moneda = string.IsNullOrEmpty(compra.Moneda) ? "PEN" : compra.Moneda;
                    compra.Subtotal = 0;
                    compra.Igv = 0;
                    compra.Total = 0;

                    _context.Compras.Add(compra);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Compra creada con IdCompra = {compra.IdCompra}");

                    // Tipo de movimiento "Entrada por Compra"
                    var tipoEntrada = await _context.TiposMovimientoInventarios
                        .FirstOrDefaultAsync(t => t.Nombre == "Entrada por Compra");

                    if (tipoEntrada == null)
                        throw new Exception("No existe el tipo de movimiento 'Entrada por Compra' en la base de datos.");

                    decimal subtotalGeneral = 0;

                    for (int i = 0; i < MaterialIds.Count; i++)
                    {
                        var cantidad = Cantidades != null && i < Cantidades.Count ? Cantidades[i] : 0;
                        var costo = Costos != null && i < Costos.Count ? Costos[i] : 0;

                        if (cantidad <= 0) continue;

                        var material = await _context.Materiales.FindAsync(MaterialIds[i]);
                        if (material == null) continue;

                        // Parsear PresentacionId
                        int? presentacionId = null;
                        if (PresentacionIdsStr != null && i < PresentacionIdsStr.Count && !string.IsNullOrWhiteSpace(PresentacionIdsStr[i]))
                        {
                            if (int.TryParse(PresentacionIdsStr[i], out int presId))
                                presentacionId = presId;
                        }

                        // Parsear UnidadCompraId
                        int? unidadCompraId = material.IdUnidadCompra;
                        if (UnidadCompraIdsStr != null && i < UnidadCompraIdsStr.Count && !string.IsNullOrWhiteSpace(UnidadCompraIdsStr[i]))
                        {
                            if (int.TryParse(UnidadCompraIdsStr[i], out int uniId))
                                unidadCompraId = uniId;
                        }

                        // Calcular factor de conversión
                        decimal factorConversion = material.FactorConversion;
                        if (presentacionId.HasValue)
                        {
                            var pres = await _context.PresentacionesMaterials.FindAsync(presentacionId.Value);
                            if (pres != null) factorConversion = pres.FactorAunidadBase;
                        }

                        // Crear detalle
                        var detalle = new DetalleCompra
                        {
                            IdCompra = compra.IdCompra,
                            IdMaterial = material.IdMaterial,
                            Cantidad = cantidad,
                            CostoUnitario = costo,
                            IdPresentacion = presentacionId,
                            IdUnidadCompra = unidadCompraId,
                            FactorConversionUsado = factorConversion,
                            Moneda = compra.Moneda
                        };
                        _context.DetalleCompras.Add(detalle);

                        subtotalGeneral += cantidad * costo;

                        // Movimiento de Kardex (el trigger actualiza el stock)
                        var kardex = new KardexInventario
                        {
                            IdMaterial = material.IdMaterial,
                            IdTipoMovimiento = tipoEntrada.IdTipoMovimiento,
                            Cantidad = cantidad * factorConversion,
                            IdCompra = compra.IdCompra,
                            IdUsuario = compra.IdUsuario,
                            DocumentoReferencia = compra.NumeroComprobante,
                            Observacion = $"Ingreso por compra #{compra.IdCompra} - {material.Nombre}",
                            FechaMovimiento = DateTime.Now,
                            CantidadUnidadOrigen = cantidad,
                            IdUnidadOrigen = unidadCompraId
                        };
                        _context.KardexInventarios.Add(kardex);

                        _logger.LogInformation($"Material {material.Nombre}: Cantidad={cantidad}, Costo={costo}, Factor={factorConversion}");
                    }

                    // Actualizar totales
                    compra.Subtotal = subtotalGeneral;
                    compra.Igv = subtotalGeneral * 0.18m;
                    compra.Total = compra.Subtotal + compra.Igv;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Compra #{compra.IdCompra} registrada exitosamente. Subtotal={compra.Subtotal}, IGV={compra.Igv}, Total={compra.Total}");

                    TempData["Success"] = $"Compra #{compra.IdCompra} registrada exitosamente. El stock se ha actualizado.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al registrar compra");
                    ModelState.AddModelError("", "Error al registrar la compra: " + ex.Message);
                }
            }

            await CargarListasAsync(compra);
            return View(compra);
        }

        // GET: Compras/Delete/5 (Anular compra)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var compra = await _context.Compras
                .Include(c => c.IdProveedorNavigation)
                .Include(c => c.DetalleCompras)
                    .ThenInclude(d => d.IdMaterialNavigation)
                .FirstOrDefaultAsync(m => m.IdCompra == id);

            if (compra == null) return NotFound();

            return View(compra);
        }

        // POST: Compras/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.DetalleCompras)
                .FirstOrDefaultAsync(c => c.IdCompra == id);

            if (compra == null) return NotFound();

            if (compra.Estado == "Anulada")
            {
                TempData["Error"] = "Esta compra ya está anulada.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var tipoSalida = await _context.TiposMovimientoInventarios
                    .FirstOrDefaultAsync(t => t.Nombre == "Ajuste Negativo");

                if (tipoSalida == null)
                    throw new Exception("No existe el tipo de movimiento 'Ajuste Negativo'.");

                foreach (var detalle in compra.DetalleCompras)
                {
                    decimal factor = detalle.FactorConversionUsado ?? 1;
                    decimal cantidadRevertir = detalle.Cantidad * factor;

                    var kardex = new KardexInventario
                    {
                        IdMaterial = detalle.IdMaterial,
                        IdTipoMovimiento = tipoSalida.IdTipoMovimiento,
                        Cantidad = cantidadRevertir,
                        IdCompra = compra.IdCompra,
                        IdUsuario = compra.IdUsuario,
                        DocumentoReferencia = $"Anulación Compra #{compra.IdCompra}",
                        Observacion = $"Anulación de compra #{compra.IdCompra}",
                        FechaMovimiento = DateTime.Now
                    };
                    _context.KardexInventarios.Add(kardex);
                }

                compra.Estado = "Anulada";
                _context.Update(compra);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Compra #{compra.IdCompra} anulada. El stock ha sido revertido.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al anular compra");
                TempData["Error"] = "Error al anular la compra: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar
        private async Task CargarListasAsync(Compra? compra = null)
        {
            ViewData["IdProveedor"] = new SelectList(
                await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync(),
                "IdProveedor", "RazonSocial", compra?.IdProveedor);

            ViewData["Materiales"] = await _context.Materiales
                .Where(m => m.Activo)
                .OrderBy(m => m.Nombre)
                .Select(m => new
                {
                    m.IdMaterial,
                    m.CodigoBarras,
                    m.Nombre,
                    m.StockActual,
                    m.IdUnidadCompra,
                    m.IdUnidadConsumo,
                    m.FactorConversion
                })
                .ToListAsync();

            ViewData["Presentaciones"] = await _context.PresentacionesMaterials
                .Where(p => p.Activo)
                .Select(p => new
                {
                    p.IdPresentacion,
                    p.IdMaterial,
                    p.Nombre,
                    p.FactorAunidadBase,
                    p.IdUnidadPresentacion
                })
                .ToListAsync();

            ViewData["UnidadesMedida"] = await _context.UnidadesMedida
                .Select(u => new { u.IdUnidad, u.Nombre, u.Abreviatura })
                .ToListAsync();
        }
    }
}
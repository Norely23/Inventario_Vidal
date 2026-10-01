using Inventario_Vidal.Data;
using Inventario_Vidal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProveedoresController> _logger;

        public ProveedoresController(ApplicationDbContext context, ILogger<ProveedoresController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index(string buscar)
        {
            var query = _context.Proveedores.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(p =>
                    p.RazonSocial.Contains(buscar) ||
                    p.Ruc.Contains(buscar) ||
                    p.CorreoElectronico.Contains(buscar));
            }

            ViewData["Buscar"] = buscar;
            var proveedores = await query.OrderBy(p => p.RazonSocial).ToListAsync();
            return View(proveedores);
        }

        // GET: Proveedores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores
                .Include(p => p.Compras)
                .FirstOrDefaultAsync(m => m.IdProveedor == id);

            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        // GET: Proveedores/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Ruc,RazonSocial,Direccion,Telefono,CorreoElectronico,ContactoNombre,CondicionesPago,Observaciones,Clasificacion,Calificacion,Activo")] Proveedore proveedor)
        {
            // Validación: RUC único (REQ-20)
            if (await _context.Proveedores.AnyAsync(p => p.Ruc == proveedor.Ruc))
            {
                ModelState.AddModelError("Ruc", "Ya existe un proveedor con este RUC.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    proveedor.FechaRegistro = DateTime.Now;
                    if (proveedor.Activo == false) proveedor.Activo = true; // Por defecto activo

                    _context.Add(proveedor);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Proveedor creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear proveedor");
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
            }
            return View(proveedor);
        }

        // GET: Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        // POST: Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdProveedor,Ruc,RazonSocial,Direccion,Telefono,CorreoElectronico,ContactoNombre,CondicionesPago,Observaciones,Clasificacion,Calificacion,Activo,FechaRegistro")] Proveedore proveedor)
        {
            if (id != proveedor.IdProveedor) return NotFound();

            // Validación: RUC único (excluyendo el propio)
            if (await _context.Proveedores.AnyAsync(p => p.Ruc == proveedor.Ruc && p.IdProveedor != id))
            {
                ModelState.AddModelError("Ruc", "Ya existe otro proveedor con este RUC.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Proveedor actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Proveedores.AnyAsync(e => e.IdProveedor == id))
                        return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al editar proveedor");
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        // GET: Proveedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(m => m.IdProveedor == id);

            if (proveedor == null) return NotFound();

            return View(proveedor);
        }

        // POST: Proveedores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                // Verificar si tiene compras asociadas (REQ-20: conservar historial)
                bool tieneCompras = await _context.Compras.AnyAsync(c => c.IdProveedor == id);

                if (tieneCompras)
                {
                    // Desactivar en lugar de eliminar (RN-25)
                    proveedor.Activo = false;
                    _context.Update(proveedor);
                    TempData["Info"] = "El proveedor tiene compras asociadas. Se ha desactivado en lugar de eliminarse.";
                }
                else
                {
                    _context.Proveedores.Remove(proveedor);
                    TempData["Success"] = "Proveedor eliminado exitosamente.";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
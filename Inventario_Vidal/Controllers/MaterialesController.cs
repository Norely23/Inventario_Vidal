using Inventario_Vidal.Data;
using Inventario_Vidal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    public class MaterialesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaterialesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Materiales
        public async Task<IActionResult> Index()
        {
            var materiales = await _context.Materiales
                .Include(m => m.IdCategoriaNavigation)
                .Include(m => m.IdMarcaNavigation)
                .OrderBy(m => m.Nombre)
                .ToListAsync();
            return View(materiales);
        }

        // GET: Materiales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.Materiales
                .Include(m => m.IdCategoriaNavigation)
                .Include(m => m.IdMarcaNavigation)
                .Include(m => m.IdUnidadCompraNavigation)
                .Include(m => m.IdUnidadConsumoNavigation)
                .FirstOrDefaultAsync(m => m.IdMaterial == id);

            if (material == null) return NotFound();

            return View(material);
        }

        // GET: Materiales/Create
        public IActionResult Create()
        {
            CargarListas();
            return View();
        }

        // POST: Materiales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CodigoBarras,Nombre,Descripcion,IdCategoria,IdMarca,IdUnidadCompra,IdUnidadConsumo,FactorConversion,EsReutilizable,EsHerramienta,StockMinimo,StockMaximo,StockActual,StockReservado,PesoUnitario,Dimensiones,ImagenUrl,Activo")] Materiale material)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    material.FechaRegistro = DateTime.Now;
                    material.FechaUltimoMovimiento = DateTime.Now;
                    _context.Add(material);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Material creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
            }
            CargarListas(material);
            return View(material);
        }

        // GET: Materiales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.Materiales.FindAsync(id);
            if (material == null) return NotFound();

            CargarListas(material);
            return View(material);
        }

        // POST: Materiales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("IdMaterial,CodigoBarras,Nombre,Descripcion,IdCategoria,IdMarca,IdUnidadCompra,IdUnidadConsumo,FactorConversion,EsReutilizable,EsHerramienta,StockMinimo,StockMaximo,StockActual,StockReservado,PesoUnitario,Dimensiones,ImagenUrl,Activo,FechaRegistro")] Materiale material)
        {
            if (id != material.IdMaterial) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    material.FechaUltimoMovimiento = DateTime.Now;
                    _context.Update(material);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Material actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaterialExists(material.IdMaterial)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            CargarListas(material);
            return View(material);
        }

        // GET: Materiales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.Materiales
                .Include(m => m.IdCategoriaNavigation)
                .Include(m => m.IdMarcaNavigation)
                .FirstOrDefaultAsync(m => m.IdMaterial == id);

            if (material == null) return NotFound();

            return View(material);
        }

        // POST: Materiales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var material = await _context.Materiales.FindAsync(id);
            if (material != null)
            {
                _context.Materiales.Remove(material);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Material eliminado exitosamente.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MaterialExists(int id)
        {
            return _context.Materiales.Any(e => e.IdMaterial == id);
        }

        private void CargarListas(Materiale? material = null)
        {
            ViewData["IdCategoria"] = new SelectList(_context.Categorias.OrderBy(c => c.Nombre), "IdCategoria", "Nombre", material?.IdCategoria);
            ViewData["IdMarca"] = new SelectList(_context.Marcas.OrderBy(m => m.Nombre), "IdMarca", "Nombre", material?.IdMarca);
            ViewData["IdUnidadCompra"] = new SelectList(_context.UnidadesMedida, "IdUnidad", "Nombre", material?.IdUnidadCompra);
            ViewData["IdUnidadConsumo"] = new SelectList(_context.UnidadesMedida, "IdUnidad", "Nombre", material?.IdUnidadConsumo);
        }
    }
}
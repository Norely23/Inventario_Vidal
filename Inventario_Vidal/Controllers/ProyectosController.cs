using Inventario_Vidal.Data;
using Inventario_Vidal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    public class ProyectosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProyectosController> _logger;

        public ProyectosController(ApplicationDbContext context, ILogger<ProyectosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Proyectos (con búsqueda y filtros)
        public async Task<IActionResult> Index(string buscar, int? estadoId)
        {
            var query = _context.Proyectos
                .Include(p => p.IdClienteNavigation)
                .Include(p => p.IdEstadoNavigation)
                .Include(p => p.IdTipoProyectoNavigation)
                .Include(p => p.IdUsuarioResponsableNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(p =>
                    p.CodigoProyecto.Contains(buscar) ||
                    (p.Descripcion != null && p.Descripcion.Contains(buscar)) ||
                    p.IdClienteNavigation.NombreOrazonSocial.Contains(buscar));
            }

            if (estadoId.HasValue && estadoId.Value > 0)
            {
                query = query.Where(p => p.IdEstado == estadoId.Value);
            }

            ViewData["Buscar"] = buscar;
            ViewData["EstadoId"] = estadoId;
            ViewData["Estados"] = new SelectList(await _context.EstadosProyectos.OrderBy(e => e.Orden).ToListAsync(), "IdEstado", "Nombre", estadoId);

            var proyectos = await query.OrderByDescending(p => p.FechaRegistro).ToListAsync();
            return View(proyectos);
        }

        // GET: Proyectos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var proyecto = await _context.Proyectos
                .Include(p => p.IdClienteNavigation)
                .Include(p => p.IdEstadoNavigation)
                .Include(p => p.IdTipoProyectoNavigation)
                .Include(p => p.IdUsuarioResponsableNavigation)
                .Include(p => p.AsignacionMaterialesPersonals)
                    .ThenInclude(a => a.IdMaterialNavigation)
                .Include(p => p.PlanosProyectos)
                .FirstOrDefaultAsync(m => m.IdProyecto == id);

            if (proyecto == null) return NotFound();

            return View(proyecto);
        }

        // GET: Proyectos/Create
        public async Task<IActionResult> Create()
        {
            await CargarListasAsync();
            return View();
        }

        // POST: Proyectos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CodigoProyecto,IdCliente,IdTipoProyecto,IdEstado,Descripcion,FechaInicio,FechaEntregaEstimada,PrecioVenta,CostoEstimado,Moneda,IdUsuarioResponsable,Observaciones,Prioridad")] Proyecto proyecto)
        {
            // Validación: código único (REQ-26)
            if (await _context.Proyectos.AnyAsync(p => p.CodigoProyecto == proyecto.CodigoProyecto))
            {
                ModelState.AddModelError("CodigoProyecto", "Ya existe un proyecto con este código.");
            }

            // Validación: fecha de entrega >= fecha de inicio
            if (proyecto.FechaEntregaEstimada.HasValue && proyecto.FechaEntregaEstimada < proyecto.FechaInicio)
            {
                ModelState.AddModelError("FechaEntregaEstimada", "La fecha de entrega no puede ser anterior a la fecha de inicio.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    proyecto.FechaRegistro = DateTime.Now;
                    if (string.IsNullOrEmpty(proyecto.Moneda)) proyecto.Moneda = "PEN";
                    if (string.IsNullOrEmpty(proyecto.Prioridad)) proyecto.Prioridad = "Media";
                    if (proyecto.CostoEstimado == 0) proyecto.CostoEstimado = 0;

                    _context.Add(proyecto);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Proyecto creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear proyecto");
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
            }

            await CargarListasAsync(proyecto);
            return View(proyecto);
        }

        // GET: Proyectos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto == null) return NotFound();

            await CargarListasAsync(proyecto);
            return View(proyecto);
        }

        // POST: Proyectos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("IdProyecto,CodigoProyecto,IdCliente,IdTipoProyecto,IdEstado,Descripcion,FechaInicio,FechaEntregaEstimada,FechaEntregaReal,PrecioVenta,CostoEstimado,CostoReal,Moneda,IdUsuarioResponsable,FechaRegistro,Observaciones,Prioridad")] Proyecto proyecto)
        {
            if (id != proyecto.IdProyecto) return NotFound();

            // Validación: código único excluyendo el propio
            if (await _context.Proyectos.AnyAsync(p => p.CodigoProyecto == proyecto.CodigoProyecto && p.IdProyecto != id))
            {
                ModelState.AddModelError("CodigoProyecto", "Ya existe otro proyecto con este código.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    proyecto.FechaActualizacion = DateTime.Now;
                    _context.Update(proyecto);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Proyecto actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Proyectos.AnyAsync(e => e.IdProyecto == id))
                        return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al editar proyecto");
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
                return RedirectToAction(nameof(Index));
            }

            await CargarListasAsync(proyecto);
            return View(proyecto);
        }

        // GET: Proyectos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var proyecto = await _context.Proyectos
                .Include(p => p.IdClienteNavigation)
                .Include(p => p.IdEstadoNavigation)
                .FirstOrDefaultAsync(m => m.IdProyecto == id);

            if (proyecto == null) return NotFound();

            return View(proyecto);
        }

        // POST: Proyectos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto != null)
            {
                // Verificar si tiene movimientos asociados (REQ-31)
                bool tieneMovimientos = await _context.KardexInventarios.AnyAsync(k => k.IdProyecto == id) ||
                                        await _context.AsignacionMaterialesPersonals.AnyAsync(a => a.IdProyecto == id);

                if (tieneMovimientos)
                {
                    TempData["Error"] = "No se puede eliminar: el proyecto tiene movimientos asociados. Ciérralo en lugar de eliminarlo.";
                }
                else
                {
                    _context.Proyectos.Remove(proyecto);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Proyecto eliminado exitosamente.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para cargar listas desplegables
        private async Task CargarListasAsync(Proyecto? proyecto = null)
        {
            ViewData["IdCliente"] = new SelectList(
                await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.NombreOrazonSocial).ToListAsync(),
                "IdCliente", "NombreOrazonSocial", proyecto?.IdCliente);

            ViewData["IdTipoProyecto"] = new SelectList(
                await _context.TiposProyectos.OrderBy(t => t.Nombre).ToListAsync(),
                "IdTipoProyecto", "Nombre", proyecto?.IdTipoProyecto);

            ViewData["IdEstado"] = new SelectList(
                await _context.EstadosProyectos.OrderBy(e => e.Orden).ToListAsync(),
                "IdEstado", "Nombre", proyecto?.IdEstado);

            ViewData["IdUsuarioResponsable"] = new SelectList(
                await _context.Usuarios.Where(u => u.Activo).OrderBy(u => u.NombreCompleto).ToListAsync(),
                "IdUsuario", "NombreCompleto", proyecto?.IdUsuarioResponsable);
        }
    }
}
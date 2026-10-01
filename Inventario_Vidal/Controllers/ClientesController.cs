using Inventario_Vidal.Data;
using Inventario_Vidal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(ApplicationDbContext context, ILogger<ClientesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Clientes (con búsqueda)
        public async Task<IActionResult> Index(string buscar)
        {
            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                query = query.Where(c =>
                    c.NombreOrazonSocial.Contains(buscar) ||
                    c.NumeroDocumento.Contains(buscar) ||
                    (c.NombreComercial != null && c.NombreComercial.Contains(buscar)));
            }

            ViewData["Buscar"] = buscar;
            var clientes = await query.OrderBy(c => c.NombreOrazonSocial).ToListAsync();
            return View(clientes);
        }

        // GET: Clientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes
                .Include(c => c.Proyectos)
                .FirstOrDefaultAsync(m => m.IdCliente == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // GET: Clientes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clientes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TipoCliente,TipoDocumento,NumeroDocumento,NombreOrazonSocial,NombreComercial,Direccion,Distrito,Provincia,Departamento,Telefono,CorreoElectronico,ContactoNombre,Observaciones,Activo")] Cliente cliente)
        {
            // Validación: documento único (REQ-104)
            if (await _context.Clientes.AnyAsync(c => c.NumeroDocumento == cliente.NumeroDocumento))
            {
                ModelState.AddModelError("NumeroDocumento", "Ya existe un cliente con este número de documento.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    cliente.FechaRegistro = DateTime.Now;
                    _context.Add(cliente);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cliente registrado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear cliente");
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
            }
            return View(cliente);
        }

        // GET: Clientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clientes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdCliente,TipoCliente,TipoDocumento,NumeroDocumento,NombreOrazonSocial,NombreComercial,Direccion,Distrito,Provincia,Departamento,Telefono,CorreoElectronico,ContactoNombre,Observaciones,Activo,FechaRegistro")] Cliente cliente)
        {
            if (id != cliente.IdCliente) return NotFound();

            // Validación: documento único (excluyendo el propio)
            if (await _context.Clientes.AnyAsync(c => c.NumeroDocumento == cliente.NumeroDocumento && c.IdCliente != id))
            {
                ModelState.AddModelError("NumeroDocumento", "Ya existe otro cliente con este documento.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cliente);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cliente actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Clientes.AnyAsync(e => e.IdCliente == id))
                        return NotFound();
                    else throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al editar cliente");
                    ModelState.AddModelError("", "Error al guardar: " + ex.Message);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cliente);
        }

        // GET: Clientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(m => m.IdCliente == id);

            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente != null)
            {
                // Verificar si tiene proyectos asociados (REQ-108)
                bool tieneProyectos = await _context.Proyectos.AnyAsync(p => p.IdCliente == id);

                if (tieneProyectos)
                {
                    cliente.Activo = false;
                    _context.Update(cliente);
                    TempData["Info"] = "El cliente tiene proyectos asociados. Se ha desactivado en lugar de eliminarse.";
                }
                else
                {
                    _context.Clientes.Remove(cliente);
                    TempData["Success"] = "Cliente eliminado exitosamente.";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
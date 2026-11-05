using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    public class MaterialesSobrantesController : Controller
    {
        private readonly IMaterialesSobrantesService _sobrantes;
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<MaterialesSobrantesController> _logger;

        public MaterialesSobrantesController(IMaterialesSobrantesService logistica, FabricacionesVidalEirlBdContext context, ILogger<MaterialesSobrantesController> logger)
        {
            _sobrantes = logistica;
            _context = context;
            _logger = logger;
        }

        private async Task<bool> TienePermisoModuloAsync(params string[] codigos)
        {
            var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario is null or <= 0) return false;
            return await _context.Usuarios.AsNoTracking().AnyAsync(u =>
                u.IdUsuario == idUsuario.Value && u.Activo && u.IdRolNavigation.Activo &&
                u.IdRolNavigation.IdPermisos.Any(p => p.Activo && codigos.Contains(p.Codigo)));
        }

        private IActionResult RedirigirAccesoDenegado()
        {
            if (HttpContext.Session.GetInt32("IdUsuario") is null or <= 0)
                return RedirectToAction("Login", "Auth");
            return RedirectToAction("AccessDenied", "Account");
        }

        private async Task CargarFiltrosAsync()
        {
            ViewBag.Proyectos = await _context.Proyectos.AsNoTracking().OrderBy(x => x.CodigoProyecto)
                .Select(x => new SelectListItem { Value = x.IdProyecto.ToString(), Text = x.CodigoProyecto }).ToListAsync();
            ViewBag.Materiales = await _context.Materiales.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre)
                .Select(x => new SelectListItem { Value = x.IdMaterial.ToString(), Text = x.Nombre }).ToListAsync();
        }

        [HttpGet]

        public async Task<IActionResult> Index(int? idMaterial, int? idProyectoOrigen)
        {
            if (!await TienePermisoModuloAsync("EQUIPO.VER", "REUTILIZACION.VER", "REUTILIZACION.GESTIONAR"))
                return RedirigirAccesoDenegado();
            await CargarFiltrosAsync();
            ViewBag.IdMaterial = idMaterial.GetValueOrDefault();
            ViewBag.IdProyectoOrigen = idProyectoOrigen.GetValueOrDefault();
            ViewBag.PuedeGestionarSobrantes = await TienePermisoModuloAsync("REUTILIZACION.GESTIONAR");
            var sobrantes = await _sobrantes.GetSobrantesDisponiblesAsync(idMaterial is > 0 ? idMaterial : null,
                idProyectoOrigen is > 0 ? idProyectoOrigen : null);
            return View(sobrantes.ToList());
        }

        // SOBRANTES: registrar solamente a partir de una asignación real.
        [HttpGet]
        [PermisoRequerido("REUTILIZACION.GESTIONAR")]
        public async Task<IActionResult> RegistrarSobrante(int? idAsignacion)
        {
            var asignaciones = await _context.AsignacionMaterialesPersonal.AsNoTracking()
                .Include(a => a.IdMaterialNavigation).Include(a => a.IdProyectoNavigation)
                .OrderByDescending(a => a.FechaAsignacion).Take(500)
                .Select(a => new SelectListItem
                {
                    Value = a.IdAsignacion.ToString(),
                    Text = "#" + a.IdAsignacion + " | " + a.IdProyectoNavigation.CodigoProyecto + " | " + a.IdMaterialNavigation.Nombre + " | Entregado: " + a.CantidadEntregada
                }).ToListAsync();
            ViewBag.Asignaciones = asignaciones;
            return View(new RegistrarSobranteLogisticaDto { IdAsignacion = idAsignacion.GetValueOrDefault() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("REUTILIZACION.GESTIONAR")]
        public async Task<IActionResult> RegistrarSobrante(RegistrarSobranteLogisticaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(new { exito = false, mensaje = "Datos del sobrante inválidos.", errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (usuarioId <= 0) return Unauthorized(new { exito = false, mensaje = "Sesión inválida." });
            try
            {
                var res = await _sobrantes.RegistrarSobranteAsync(dto, usuarioId);
                if (!res.Exito) return BadRequest(new { exito = false, mensaje = res.Mensaje });
                return Ok(new { exito = true, mensaje = res.Mensaje, idRecuperado = res.IdRecuperado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando sobrante desde asignación {Id}", dto.IdAsignacion);
                return StatusCode(500, new { exito = false, mensaje = "No se pudo registrar el sobrante." });
            }
        }

        [HttpGet]
        [PermisoRequerido("REUTILIZACION.GESTIONAR")]
        public async Task<IActionResult> ReasignarRecuperado(long id)
        {
            if (id <= 0) return BadRequest();
            var sobrante = await _sobrantes.GetSobranteByIdAsync(id);
            if (sobrante == null) return NotFound();
            if (sobrante.Estado != "Disponible" || sobrante.CantidadDisponible <= 0)
            {
                TempData["MensajeError"] = "El sobrante no está disponible para su reutilización.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Sobrante = sobrante;
            ViewBag.Proyectos = await _context.Proyectos.AsNoTracking()
                .Where(x => x.IdProyecto != sobrante.IdProyectoOrigen)
                .OrderBy(x => x.CodigoProyecto)
                .Select(x => new SelectListItem { Value = x.IdProyecto.ToString(), Text = x.CodigoProyecto })
                .ToListAsync();
            return View(new ReasignarRecuperadoLogisticaDto { IdRecuperado = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("REUTILIZACION.GESTIONAR")]
        public async Task<IActionResult> ReasignarRecuperado(ReasignarRecuperadoLogisticaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(new { exito = false, mensaje = "Datos de reasignación inválidos.", errores = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
            var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (usuarioId <= 0) return Unauthorized(new { exito = false, mensaje = "Sesión inválida." });
            try
            {
                var receptorActivo = await _context.AsignacionPersonalProyectos.AsNoTracking().AnyAsync(a =>
                    a.IdProyecto == dto.IdProyectoDestino && a.FechaHoraFin == null &&
                    ((dto.IdEmpleadoDestino.HasValue && a.IdEmpleado == dto.IdEmpleadoDestino && a.IdEmpleadoNavigation != null && a.IdEmpleadoNavigation.Activo) ||
                     (dto.IdContratistaDestino.HasValue && a.IdContratista == dto.IdContratistaDestino && a.IdContratistaNavigation != null && a.IdContratistaNavigation.Activo)));
                if (!receptorActivo) return BadRequest(new { exito = false, mensaje = "El destinatario debe estar activo y asignado al proyecto destino." });
                var res = await _sobrantes.ReasignarRecuperadoAsync(dto, usuarioId);
                if (!res.Exito) return BadRequest(new { exito = false, mensaje = res.Mensaje });
                return Ok(new { exito = true, mensaje = res.Mensaje, idEntrega = res.IdEntrega, idAsignacion = res.IdAsignacion });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reasignando material recuperado {Id}", dto.IdRecuperado);
                return StatusCode(500, new { exito = false, mensaje = "No se pudo completar la reasignación." });
            }
        }

    }
}

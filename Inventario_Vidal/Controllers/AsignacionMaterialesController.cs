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
    public class AsignacionMaterialesController : Controller
    {
        private readonly IAsignacionMaterialesService _entregas;
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<AsignacionMaterialesController> _logger;

        public AsignacionMaterialesController(IAsignacionMaterialesService logistica, FabricacionesVidalEirlBdContext context, ILogger<AsignacionMaterialesController> logger)
        {
            _entregas = logistica;
            _context = context;
            _logger = logger;
        }

        [HttpGet]

        public async Task<IActionResult> Index()
        {
            if (!await TienePermisoModuloAsync("EQUIPO.VER", "ENTREGA.CREAR"))
                return RedirigirAccesoDenegado();
            // Actividad global: no excluir proyectos antiguos por su identificador.
            var entregas = await _entregas.GetUltimasEntregasAsync();
            ViewBag.PuedeEntregar = await TienePermisoModuloAsync("ENTREGA.CREAR");
            ViewBag.PuedeHistorial = await TienePermisoModuloAsync("EQUIPO.VER", "ENTREGA.CREAR");
            return View(entregas.OrderByDescending(e => e.FechaEntrega).Take(30).ToList());
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

        public async Task<IActionResult> Historial(int? idProyecto)
        {
            if (!await TienePermisoModuloAsync("EQUIPO.VER", "ENTREGA.CREAR"))
                return RedirigirAccesoDenegado();
            await CargarFiltrosAsync();
            ViewBag.IdProyecto = idProyecto.GetValueOrDefault();
            var entregas = idProyecto is > 0
                ? await _entregas.GetEntregasByProyectoAsync(idProyecto.Value)
                : await _entregas.GetUltimasEntregasAsync();
            return View(entregas.OrderByDescending(e => e.FechaEntrega).ToList());
        }

        [HttpGet]

        public async Task<IActionResult> DetalleEntrega(long id)
        {
            if (!await TienePermisoModuloAsync("EQUIPO.VER", "ENTREGA.CREAR"))
                return RedirigirAccesoDenegado();
            if (id <= 0) return BadRequest();
            var entrega = await _entregas.GetEntregaByIdAsync(id);
            return entrega == null ? NotFound() : View(entrega);
        }

        [HttpGet]
        [PermisoRequerido("ENTREGA.CREAR")]
        public async Task<IActionResult> RegistrarEntrega()
        {
            ViewBag.Proyectos = await _context.Proyectos.AsNoTracking()
                .OrderBy(p => p.CodigoProyecto)
                .Select(p => new SelectListItem { Value = p.IdProyecto.ToString(), Text = p.CodigoProyecto })
                .ToListAsync();
            ViewBag.Materiales = await _context.Materiales.AsNoTracking()
                .Where(m => m.Activo)
                .OrderBy(m => m.Nombre)
                .Select(m => new SelectListItem { Value = m.IdMaterial.ToString(), Text = m.Nombre })
                .ToListAsync();
            ViewBag.Empleados = new List<SelectListItem>();
            ViewBag.Contratistas = new List<SelectListItem>();
            return View();
        }

        // Códigos de materiales: coincidencia exacta con CodigoBarras.
        // Etiquetas de paquete: VIDAL-PQ-<IdPaquete> o PKG:<IdPaquete>.
        [HttpGet]
        [PermisoRequerido("ENTREGA.CREAR")]
        public async Task<IActionResult> IdentificarCodigo(string codigo)
        {
            codigo = (codigo ?? string.Empty).Trim();
            if (codigo.Length == 0 || codigo.Length > 100)
                return BadRequest(new { mensaje = "Código vacío o demasiado largo." });

            if (codigo.StartsWith("VIDAL-PQ-", StringComparison.OrdinalIgnoreCase) ||
                codigo.StartsWith("PKG:", StringComparison.OrdinalIgnoreCase))
            {
                var identificador = codigo.StartsWith("PKG:", StringComparison.OrdinalIgnoreCase)
                    ? codigo[4..] : codigo[9..];
                if (!long.TryParse(identificador, out var idPaquete) || idPaquete <= 0)
                    return BadRequest(new { mensaje = "Código de paquete inválido." });
                var paquete = await _context.PaquetesInventarios.AsNoTracking()
                    .Where(p => p.IdPaquete == idPaquete && p.CantidadActualUnidadBase > 0 &&
                        (p.Estado == "Abierto" || p.Estado == "Sellado"))
                    .Select(p => new {
                        p.IdPaquete,
                        p.IdMaterial,
                        nombre = p.IdMaterialNavigation.Nombre,
                        p.CantidadActualUnidadBase
                    }).FirstOrDefaultAsync();
                if (paquete == null) return NotFound(new { mensaje = "Paquete inexistente o no disponible." });
                return Json(new
                {
                    tipo = "paquete",
                    paquete.IdPaquete,
                    paquete.IdMaterial,
                    paquete.nombre,
                    disponible = paquete.CantidadActualUnidadBase
                });
            }

            var material = await _context.Materiales.AsNoTracking()
                .Where(m => m.Activo && m.CodigoBarras == codigo)
                .Select(m => new { m.IdMaterial, m.Nombre, m.CodigoBarras }).FirstOrDefaultAsync();
            if (material == null) return NotFound(new { mensaje = "No existe un material activo con ese código." });
            return Json(new
            {
                tipo = "material",
                material.IdMaterial,
                nombre = material.Nombre,
                material.CodigoBarras
            });
        }

        // Sugerencias seguras: solo materiales activos; no se recuperan paquetes aquí.
        [HttpGet]
        [PermisoRequerido("ENTREGA.CREAR")]
        public async Task<IActionResult> BuscarMateriales(string termino)
        {
            termino = (termino ?? string.Empty).Trim();
            if (termino.Length < 2 || termino.Length > 100)
                return Json(Array.Empty<object>());

            var coincidencias = await _context.Materiales.AsNoTracking()
                .Where(m => m.Activo &&
                    (m.Nombre.Contains(termino) || (m.CodigoBarras != null && m.CodigoBarras.Contains(termino))))
                .OrderBy(m => m.Nombre).ThenBy(m => m.IdMaterial)
                .Take(15)
                .Select(m => new { idMaterial = m.IdMaterial, nombre = m.Nombre, codigo = m.CodigoBarras })
                .ToListAsync();
            return Json(coincidencias);
        }

        [HttpGet]

        public async Task<IActionResult> PersonasProyecto(int idProyecto, string tipo)
        {
            if (!await TienePermisoModuloAsync("ENTREGA.CREAR", "REUTILIZACION.GESTIONAR"))
                return RedirigirAccesoDenegado();
            if (idProyecto <= 0 || (tipo != "Empleado" && tipo != "Contratista"))
                return BadRequest(new { mensaje = "Proyecto o tipo de receptor inválido." });

            if (tipo == "Empleado")
            {
                var personas = await _context.AsignacionPersonalProyectos.AsNoTracking()
                    .Where(a => a.IdProyecto == idProyecto && a.FechaHoraFin == null && a.IdEmpleado != null && a.IdEmpleadoNavigation != null && a.IdEmpleadoNavigation.Activo)
                    .Select(a => new { value = a.IdEmpleado!.Value, text = a.IdEmpleadoNavigation!.NombreCompleto + " - " + a.IdEmpleadoNavigation.NumeroDocumento })
                    .Distinct().OrderBy(x => x.text).ToListAsync();
                return Json(personas);
            }

            var contratistas = await _context.AsignacionPersonalProyectos.AsNoTracking()
                .Where(a => a.IdProyecto == idProyecto && a.FechaHoraFin == null && a.IdContratista != null && a.IdContratistaNavigation != null && a.IdContratistaNavigation.Activo)
                .Select(a => new { value = a.IdContratista!.Value, text = a.IdContratistaNavigation!.NombreCompleto + " - " + a.IdContratistaNavigation.NumeroDocumento })
                .Distinct().OrderBy(x => x.text).ToListAsync();
            return Json(contratistas);
        }

        [HttpGet]
        [PermisoRequerido("ENTREGA.CREAR")]
        public async Task<IActionResult> Presentaciones(int idMaterial)
        {
            if (idMaterial <= 0) return BadRequest(new { mensaje = "Material inválido." });
            try
            {
                var resultado = await _entregas.GetPresentacionesByMaterialAsync(idMaterial);
                return Json(resultado.Where(p => p.Activo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar presentaciones del material {IdMaterial}", idMaterial);
                return StatusCode(500, new { mensaje = "No se pudieron cargar las presentaciones." });
            }
        }

        [HttpGet]
        [PermisoRequerido("ENTREGA.CREAR")]
        public async Task<IActionResult> PaquetesDisponibles(int idMaterial)
        {
            if (idMaterial <= 0) return BadRequest(new { mensaje = "Material inválido." });
            try { return Json(await _entregas.GetPaquetesDisponiblesAsync(idMaterial)); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar paquetes del material {IdMaterial}", idMaterial);
                return StatusCode(500, new { mensaje = "No se pudieron cargar los paquetes." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("ENTREGA.CREAR")]
        public async Task<IActionResult> RegistrarEntrega([FromBody] RegistrarEntregaLogisticaDto dto)
        {
            if (dto == null || !ModelState.IsValid)
                return BadRequest(new { exito = false, mensaje = "Revise los datos de la entrega.", errores = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).ToArray() });

            var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            if (usuarioId <= 0) return Unauthorized(new { exito = false, mensaje = "Sesión no válida." });

            var receptorAsignado = await _context.AsignacionPersonalProyectos.AsNoTracking().AnyAsync(a =>
                a.IdProyecto == dto.IdProyecto && a.FechaHoraFin == null &&
                ((dto.IdEmpleado.HasValue && a.IdEmpleado == dto.IdEmpleado && a.IdEmpleadoNavigation != null && a.IdEmpleadoNavigation.Activo) ||
                 (dto.IdContratista.HasValue && a.IdContratista == dto.IdContratista && a.IdContratistaNavigation != null && a.IdContratistaNavigation.Activo)));
            if (!receptorAsignado)
                return BadRequest(new { exito = false, mensaje = "El receptor no está asignado y activo en el proyecto seleccionado." });

            try
            {
                var resultado = await _entregas.RegistrarEntregaAsync(dto, usuarioId);
                if (!resultado.Exito) return BadRequest(new { exito = false, mensaje = resultado.Mensaje });
                return Ok(new { exito = true, mensaje = resultado.Mensaje, idEntrega = resultado.IdEntrega });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrega al proyecto {IdProyecto}", dto.IdProyecto);
                return StatusCode(500, new { exito = false, mensaje = "No fue posible registrar la entrega." });
            }
        }

    }
}

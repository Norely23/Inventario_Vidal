using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class MaquinariasController : Controller
    {
        private readonly IMaquinariaService _maquinariaService;
        private readonly IProyectoService _proyectoService;
        private readonly IContratistaService _contratistaService;
        private readonly IEmpleadoService _empleadoService;
        private readonly IImagenUploadService _imagenUploadService;
        private readonly ILogger<MaquinariasController> _logger;

        public MaquinariasController(
            IMaquinariaService maquinariaService,
            IProyectoService proyectoService,
            IContratistaService contratistaService,
            IEmpleadoService empleadoService,
            IImagenUploadService imagenUploadService,
            ILogger<MaquinariasController> logger)
        {
            _maquinariaService = maquinariaService;
            _proyectoService = proyectoService;
            _contratistaService = contratistaService;
            _empleadoService = empleadoService;
            _imagenUploadService = imagenUploadService;
            _logger = logger;
        }

        private bool TryObtenerUsuarioId(out int usuarioId)
        {
            usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            return usuarioId > 0;
        }

        private const string MensajeSesionInvalida = "No se pudo identificar al usuario de la sesión. Inicie sesión nuevamente.";

        // ================================================================
        // MAQUINARIAS - INDEX
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> Index(MaquinariaFiltroDto? filtro = null)
        {
            try
            {
                IEnumerable<MaquinariaListDto> maquinarias;

                if (filtro?.SoloDisponibles == true)
                {
                    maquinarias = await _maquinariaService.GetMaquinariasDisponiblesAsync();
                }
                else if (filtro?.SoloActivos == true)
                {
                    maquinarias = await _maquinariaService.GetMaquinariasActivasAsync();
                }
                else
                {
                    maquinarias = await _maquinariaService.GetMaquinariasAsync();
                }

                if (!string.IsNullOrEmpty(filtro?.Buscar))
                {
                    var busqueda = filtro.Buscar.ToLower();
                    maquinarias = maquinarias.Where(m =>
                        m.Nombre.ToLower().Contains(busqueda) ||
                        m.Codigo.ToLower().Contains(busqueda) ||
                        (m.Marca != null && m.Marca.ToLower().Contains(busqueda)) ||
                        (m.Modelo != null && m.Modelo.ToLower().Contains(busqueda)));
                }

                if (filtro?.IdEstadoMaquinaria.HasValue == true)
                {
                    maquinarias = maquinarias.Where(m => m.IdEstadoMaquinaria == filtro.IdEstadoMaquinaria.Value);
                }

                ViewBag.Estados = await _maquinariaService.GetEstadosMaquinariaAsync();
                ViewBag.Filtro = filtro ?? new MaquinariaFiltroDto();

                return View(maquinarias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar maquinarias");
                TempData["MensajeError"] = "Error al cargar las maquinarias";
                return View(new List<MaquinariaListDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.GESTIONAR")]
        public async Task<IActionResult> Crear()
        {
            try
            {
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync();
                ViewBag.CodigoPrevisto = await _maquinariaService.GetProximoCodigoMaquinariaAsync();
                return View(new MaquinariaCreateDto
                {
                    Activo = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de creación de maquinaria");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MAQUINARIA.GESTIONAR")]
        public async Task<IActionResult> Crear(MaquinariaCreateDto model)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
            {
                TempData["MensajeError"] = MensajeSesionInvalida;
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync();
                ViewBag.CodigoPrevisto = await _maquinariaService.GetProximoCodigoMaquinariaAsync();
                return View(model);
            }

            var subida = await _imagenUploadService.SubirImagenAsync(model.ImagenArchivo, "maquinarias");
            if (!subida.Success)
            {
                ModelState.AddModelError("ImagenArchivo", subida.Message);
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync();
                ViewBag.CodigoPrevisto = await _maquinariaService.GetProximoCodigoMaquinariaAsync();
                return View(model);
            }
            model.ImagenUrl = subida.RutaRelativa;

            try
            {
                var result = await _maquinariaService.CreateMaquinariaAsync(model, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Index");
                }

                TempData["MensajeError"] = result.Message;
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync();
                ViewBag.CodigoPrevisto = await _maquinariaService.GetProximoCodigoMaquinariaAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear maquinaria");
                TempData["MensajeError"] = "Error al crear la maquinaria";
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync();
                ViewBag.CodigoPrevisto = await _maquinariaService.GetProximoCodigoMaquinariaAsync();
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.GESTIONAR")]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var maquinaria = await _maquinariaService.GetMaquinariaByIdAsync(id);
                if (maquinaria == null)
                {
                    TempData["MensajeError"] = "Maquinaria no encontrada";
                    return RedirectToAction("Index");
                }

                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync(maquinaria.IdEstadoMaquinaria);

                var model = new MaquinariaEditDto
                {
                    IdMaquinaria = maquinaria.IdMaquinaria,
                    Codigo = maquinaria.Codigo,
                    Nombre = maquinaria.Nombre,
                    Descripcion = maquinaria.Descripcion,
                    Marca = maquinaria.Marca,
                    Modelo = maquinaria.Modelo,
                    NumeroSerie = maquinaria.NumeroSerie,
                    IdEstadoMaquinaria = maquinaria.IdEstadoMaquinaria,
                    FechaAdquisicion = maquinaria.FechaAdquisicion,
                    ImagenUrl = maquinaria.ImagenUrl,
                    Activo = maquinaria.Activo
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar maquinaria para editar");
                TempData["MensajeError"] = "Error al cargar la maquinaria";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MAQUINARIA.GESTIONAR")]
        public async Task<IActionResult> Editar(MaquinariaEditDto model)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
            {
                TempData["MensajeError"] = MensajeSesionInvalida;
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync(model.IdEstadoMaquinaria);
                return View(model);
            }

            var subida = await _imagenUploadService.SubirImagenAsync(model.ImagenArchivo, "maquinarias", model.ImagenUrl);
            if (!subida.Success)
            {
                ModelState.AddModelError("ImagenArchivo", subida.Message);
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync(model.IdEstadoMaquinaria);
                return View(model);
            }
            model.ImagenUrl = subida.RutaRelativa;

            try
            {
                var result = await _maquinariaService.UpdateMaquinariaAsync(model, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Index");
                }

                TempData["MensajeError"] = result.Message;
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync(model.IdEstadoMaquinaria);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar maquinaria");
                TempData["MensajeError"] = "Error al actualizar la maquinaria";
                ViewBag.Estados = await _maquinariaService.GetEstadosAsignablesAsync(model.IdEstadoMaquinaria);
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var maquinaria = await _maquinariaService.GetMaquinariaByIdAsync(id);
                if (maquinaria == null)
                {
                    TempData["MensajeError"] = "Maquinaria no encontrada";
                    return RedirectToAction("Index");
                }

                var prestamos = await _maquinariaService.GetHistorialPrestamosAsync(null, null, id);
                var mantenimientos = await _maquinariaService.GetHistorialMantenimientosAsync(id);

                ViewBag.Prestamos = prestamos;
                ViewBag.Mantenimientos = mantenimientos;

                return View(maquinaria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles de la maquinaria");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ACTIVAR/DESACTIVAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MAQUINARIA.GESTIONAR")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
            {
                TempData["MensajeError"] = MensajeSesionInvalida;
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _maquinariaService.ToggleActivoAsync(id, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado de la maquinaria {Id}", id);
                TempData["MensajeError"] = "Error al cambiar el estado de la maquinaria";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ETIQUETA INDIVIDUAL - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> EtiquetaIndividual(int id)
        {
            try
            {
                var maquinaria = await _maquinariaService.GetMaquinariaByIdAsync(id);
                if (maquinaria == null)
                {
                    TempData["MensajeError"] = "Maquinaria no encontrada";
                    return RedirectToAction("Index");
                }

                var config = await GetConfiguracionEmpresa();

                ViewBag.Empresa = config;
                ViewBag.Fecha = DateTime.Now;

                return View(maquinaria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar etiqueta individual de la maquinaria {Id}", id);
                TempData["MensajeError"] = "Error al cargar la etiqueta";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // ETIQUETAS MÚLTIPLES - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> EtiquetasMultiples(string ids)
        {
            try
            {
                if (string.IsNullOrEmpty(ids))
                {
                    TempData["MensajeError"] = "No se seleccionaron maquinarias";
                    return RedirectToAction("Index");
                }

                var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => int.TryParse(x, out var n) ? n : 0)
                    .Where(n => n > 0)
                    .Distinct()
                    .ToList();
                var maquinarias = new List<MaquinariaDetailDto>();

                foreach (var id in idList)
                {
                    var maquinaria = await _maquinariaService.GetMaquinariaByIdAsync(id);
                    if (maquinaria != null)
                    {
                        maquinarias.Add(maquinaria);
                    }
                }

                if (!maquinarias.Any())
                {
                    TempData["MensajeError"] = "No se encontraron maquinarias";
                    return RedirectToAction("Index");
                }

                var config = await GetConfiguracionEmpresa();

                ViewBag.Empresa = config;
                ViewBag.Fecha = DateTime.Now;

                return View(maquinarias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar etiquetas múltiples");
                TempData["MensajeError"] = "Error al cargar las etiquetas";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // PRÉSTAMOS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> Prestamos(DateTime? desde = null, DateTime? hasta = null)
        {
            try
            {
                var prestamos = await _maquinariaService.GetHistorialPrestamosAsync(desde, hasta);
                ViewBag.Desde = desde;
                ViewBag.Hasta = hasta;
                return View("Prestamos/Index", prestamos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar préstamos");
                TempData["MensajeError"] = "Error al cargar los préstamos";
                return View("Prestamos/Index", new List<PrestamoMaquinariaListDto>());
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.PRESTAR")]
        public async Task<IActionResult> RegistrarPrestamo(int? idMaquinaria = null)
        {
            try
            {
                var disponibles = (await _maquinariaService.GetMaquinariasDisponiblesAsync()).ToList();
                ViewBag.Maquinarias = disponibles;
                ViewBag.Proyectos = await _proyectoService.GetProyectosAsync();
                ViewBag.Contratistas = await _contratistaService.GetContratistasActivosAsync();
                ViewBag.Empleados = await _empleadoService.GetEmpleadosActivosAsync();

                var model = new PrestamoMaquinariaCreateDto();

                if (idMaquinaria.HasValue)
                {
                    if (disponibles.Any(m => m.IdMaquinaria == idMaquinaria.Value))
                    {
                        model.IdMaquinaria = idMaquinaria.Value;
                    }
                    else
                    {
                        TempData["MensajeError"] = "La maquinaria seleccionada no está disponible para préstamo";
                    }
                }

                return View("Prestamos/Registrar", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de registro de préstamo");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Prestamos");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MAQUINARIA.PRESTAR")]
        public async Task<IActionResult> RegistrarPrestamo(PrestamoMaquinariaCreateDto model)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
            {
                TempData["MensajeError"] = MensajeSesionInvalida;
                return RedirectToAction("Prestamos");
            }

            if (model.IdMaquinaria <= 0)
            {
                ModelState.AddModelError(nameof(model.IdMaquinaria), "Seleccione una maquinaria");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Maquinarias = await _maquinariaService.GetMaquinariasDisponiblesAsync();
                ViewBag.Proyectos = await _proyectoService.GetProyectosAsync();
                ViewBag.Contratistas = await _contratistaService.GetContratistasActivosAsync();
                ViewBag.Empleados = await _empleadoService.GetEmpleadosActivosAsync();
                return View("Prestamos/Registrar", model);
            }

            try
            {
                var result = await _maquinariaService.RegistrarPrestamoAsync(model, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Prestamos");
                }

                TempData["MensajeError"] = result.Message;
                ViewBag.Maquinarias = await _maquinariaService.GetMaquinariasDisponiblesAsync();
                ViewBag.Proyectos = await _proyectoService.GetProyectosAsync();
                ViewBag.Contratistas = await _contratistaService.GetContratistasActivosAsync();
                ViewBag.Empleados = await _empleadoService.GetEmpleadosActivosAsync();
                return View("Prestamos/Registrar", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar préstamo");
                TempData["MensajeError"] = "Error al registrar el préstamo";
                ViewBag.Maquinarias = await _maquinariaService.GetMaquinariasDisponiblesAsync();
                ViewBag.Proyectos = await _proyectoService.GetProyectosAsync();
                ViewBag.Contratistas = await _contratistaService.GetContratistasActivosAsync();
                ViewBag.Empleados = await _empleadoService.GetEmpleadosActivosAsync();
                return View("Prestamos/Registrar", model);
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.DEVOLVER")]
        public async Task<IActionResult> DevolverPrestamo(int id)
        {
            try
            {
                var prestamo = await _maquinariaService.GetPrestamoByIdAsync(id);

                if (prestamo == null)
                {
                    TempData["MensajeError"] = "Préstamo no encontrado";
                    return RedirectToAction("Prestamos");
                }

                if (prestamo.FechaDevolucionReal.HasValue)
                {
                    TempData["MensajeError"] = "El préstamo ya fue devuelto";
                    return RedirectToAction("DetallesPrestamo", new { id });
                }

                return View("Prestamos/Devolver", prestamo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de devolución de préstamo");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Prestamos");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MAQUINARIA.DEVOLVER")]
        public async Task<IActionResult> DevolverPrestamo(int idPrestamo, bool requiereMantenimiento = false, string? estadoFisico = null, string? observaciones = null)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
            {
                TempData["MensajeError"] = MensajeSesionInvalida;
                return RedirectToAction("Prestamos");
            }

            try
            {
                var result = await _maquinariaService.RegistrarDevolucionAsync(idPrestamo, requiereMantenimiento, usuarioId, estadoFisico, observaciones);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction("Prestamos");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al devolver préstamo {IdPrestamo}", idPrestamo);
                TempData["MensajeError"] = "Error al devolver el préstamo";
                return RedirectToAction("Prestamos");
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> DetallesPrestamo(int id)
        {
            try
            {
                var prestamo = await _maquinariaService.GetPrestamoByIdAsync(id);

                if (prestamo == null)
                {
                    TempData["MensajeError"] = "Préstamo no encontrado";
                    return RedirectToAction("Prestamos");
                }

                return View("Prestamos/Detalles", prestamo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del préstamo {Id}", id);
                TempData["MensajeError"] = "Error al cargar los detalles del préstamo";
                return RedirectToAction("Prestamos");
            }
        }

        // ================================================================
        // MANTENIMIENTO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> Mantenimiento(int? idMaquinaria = null)
        {
            try
            {
                var mantenimientos = await _maquinariaService.GetHistorialMantenimientosAsync(idMaquinaria);
                var maquinarias = await _maquinariaService.GetMaquinariasActivasAsync();

                ViewBag.Maquinarias = maquinarias;
                ViewBag.IdMaquinaria = idMaquinaria;

                return View("Mantenimiento/Index", mantenimientos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar mantenimientos");
                TempData["MensajeError"] = "Error al cargar los mantenimientos";
                return View("Mantenimiento/Index", new List<MantenimientoMaquinariaListDto>());
            }
        }

        [HttpGet]
        [PermisoRequerido("MANTENIMIENTO.GESTIONAR")]
        public async Task<IActionResult> RegistrarMantenimiento(int? idMaquinaria = null)
        {
            try
            {
                var maquinarias = (await _maquinariaService.GetMaquinariasActivasAsync()).ToList();
                ViewBag.Maquinarias = maquinarias;

                var model = new MantenimientoMaquinariaCreateDto
                {
                    TipoMantenimiento = "Preventivo",
                    Moneda = "PEN",
                    FechaInicio = DateOnly.FromDateTime(DateTime.Now)
                };

                if (idMaquinaria.HasValue)
                {
                    if (maquinarias.Any(m => m.IdMaquinaria == idMaquinaria.Value))
                    {
                        model.IdMaquinaria = idMaquinaria.Value;
                    }
                    else
                    {
                        TempData["MensajeError"] = "La maquinaria seleccionada no existe o está inactiva";
                    }
                }

                return View("Mantenimiento/Registrar", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de registro de mantenimiento");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Mantenimiento");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MANTENIMIENTO.GESTIONAR")]
        public async Task<IActionResult> RegistrarMantenimiento(MantenimientoMaquinariaCreateDto model)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
            {
                TempData["MensajeError"] = MensajeSesionInvalida;
                return RedirectToAction("Mantenimiento");
            }

            if (model.IdMaquinaria <= 0)
            {
                ModelState.AddModelError(nameof(model.IdMaquinaria), "Seleccione una maquinaria");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Maquinarias = await _maquinariaService.GetMaquinariasActivasAsync();
                return View("Mantenimiento/Registrar", model);
            }

            try
            {
                var result = await _maquinariaService.RegistrarMantenimientoAsync(model, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Mantenimiento");
                }

                TempData["MensajeError"] = result.Message;
                ViewBag.Maquinarias = await _maquinariaService.GetMaquinariasActivasAsync();
                return View("Mantenimiento/Registrar", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar mantenimiento");
                TempData["MensajeError"] = "Error al registrar el mantenimiento";
                ViewBag.Maquinarias = await _maquinariaService.GetMaquinariasActivasAsync();
                return View("Mantenimiento/Registrar", model);
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> DetallesMantenimiento(int id)
        {
            try
            {
                var mantenimiento = await _maquinariaService.GetMantenimientoByIdAsync(id);

                if (mantenimiento == null)
                {
                    TempData["MensajeError"] = "Mantenimiento no encontrado";
                    return RedirectToAction("Mantenimiento");
                }

                return View("Mantenimiento/Detalles", mantenimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del mantenimiento {Id}", id);
                TempData["MensajeError"] = "Error al cargar los detalles del mantenimiento";
                return RedirectToAction("Mantenimiento");
            }
        }

        [HttpGet]
        [PermisoRequerido("MANTENIMIENTO.GESTIONAR")]
        public async Task<IActionResult> CompletarMantenimiento(int id)
        {
            try
            {
                var mantenimiento = await _maquinariaService.GetMantenimientoByIdAsync(id);

                if (mantenimiento == null)
                {
                    TempData["MensajeError"] = "Mantenimiento no encontrado";
                    return RedirectToAction("Mantenimiento");
                }

                if (mantenimiento.FechaFin.HasValue)
                {
                    TempData["MensajeError"] = "El mantenimiento ya fue completado";
                    return RedirectToAction("DetallesMantenimiento", new { id });
                }

                return View("Mantenimiento/Completar", mantenimiento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de finalización de mantenimiento");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Mantenimiento");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MANTENIMIENTO.GESTIONAR")]
        public async Task<IActionResult> CompletarMantenimiento(int id, string? observaciones, string? estadoFinal = null, decimal? costoFinal = null, string? moneda = null)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
            {
                TempData["MensajeError"] = MensajeSesionInvalida;
                return RedirectToAction("Mantenimiento");
            }

            try
            {
                var result = await _maquinariaService.CompletarMantenimientoAsync(id, observaciones, usuarioId, estadoFinal, costoFinal, moneda);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction("Mantenimiento");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al completar mantenimiento {Id}", id);
                TempData["MensajeError"] = "Error al completar el mantenimiento";
                return RedirectToAction("Mantenimiento");
            }
        }

        // ================================================================
        // MÉTODOS PRIVADOS
        // ================================================================

        private async Task<EmpresaDto?> GetConfiguracionEmpresa()
        {
            try
            {
                var empresaService = HttpContext.RequestServices.GetRequiredService<IEmpresaService>();
                return await empresaService.GetConfiguracionAsync();
            }
            catch
            {
                return null;
            }
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> GetMaquinariasSelect(string? term = null)
        {
            try
            {
                var maquinarias = await _maquinariaService.GetMaquinariasActivasAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    maquinarias = maquinarias.Where(m =>
                        m.Nombre.ToLower().Contains(term) ||
                        m.Codigo.ToLower().Contains(term));
                }

                var result = maquinarias.Select(m => new
                {
                    m.IdMaquinaria,
                    m.Codigo,
                    m.Nombre,
                    m.Estado,
                    display = $"{m.Codigo} - {m.Nombre}"
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener maquinarias para select");
                return Json(new { success = false, message = "Error al cargar maquinarias" });
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> GetMaquinariasDisponiblesSelect(string? term = null)
        {
            try
            {
                var maquinarias = await _maquinariaService.GetMaquinariasDisponiblesAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    maquinarias = maquinarias.Where(m =>
                        m.Nombre.ToLower().Contains(term) ||
                        m.Codigo.ToLower().Contains(term));
                }

                var result = maquinarias.Select(m => new
                {
                    m.IdMaquinaria,
                    m.Codigo,
                    m.Nombre,
                    display = $"{m.Codigo} - {m.Nombre}"
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener maquinarias disponibles para select");
                return Json(new { success = false, message = "Error al cargar maquinarias" });
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> GetEstadosMaquinaria()
        {
            try
            {
                var estados = await _maquinariaService.GetEstadosMaquinariaAsync();

                return Json(new
                {
                    success = true,
                    data = estados.Select(e => new
                    {
                        e.IdEstadoMaquinaria,
                        e.Nombre
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estados de maquinaria");
                return Json(new { success = false, message = "Error al cargar estados" });
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> GetPrestamosActivos()
        {
            try
            {
                var prestamos = await _maquinariaService.GetPrestamosActivosAsync();

                return Json(new
                {
                    success = true,
                    total = prestamos.Count(),
                    data = prestamos.Select(p => new
                    {
                        p.IdPrestamo,
                        p.Maquinaria,
                        p.CodigoMaquinaria,
                        p.Destinatario,
                        p.FechaPrestamo,
                        p.FechaDevolucionEstimada,
                        p.Estado,
                        p.EstadoBadge
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener préstamos activos");
                return Json(new { success = false, message = "Error al cargar préstamos" });
            }
        }

        [HttpGet]
        [PermisoRequerido("MAQUINARIA.VER")]
        public async Task<IActionResult> GetMantenimientosActivos()
        {
            try
            {
                var activos = (await _maquinariaService.GetMantenimientosActivosAsync()).ToList();

                return Json(new
                {
                    success = true,
                    total = activos.Count(),
                    data = activos.Select(m => new
                    {
                        m.IdMantenimiento,
                        m.Maquinaria,
                        m.CodigoMaquinaria,
                        m.TipoMantenimiento,
                        m.FechaInicio,
                        m.Descripcion
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener mantenimientos activos");
                return Json(new { success = false, message = "Error al cargar mantenimientos" });
            }
        }
    }
}
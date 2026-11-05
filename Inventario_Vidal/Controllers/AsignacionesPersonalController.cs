using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    public class AsignacionesPersonalController : Controller
    {
        private readonly IAsignacionesPersonalService _equipoService;
        private readonly IAsignacionMaterialesService _entregasService;
        private readonly IEmpleadoService _empleadoService;
        private readonly IContratistaService _contratistaService;
        private readonly IProyectoService _proyectoService;
        private readonly IReporteService _reporteService;
        private readonly IReportePdfService _reportePdfService;
        private readonly ILogger<AsignacionesPersonalController> _logger;

        public AsignacionesPersonalController(
            IAsignacionesPersonalService equipoService,
            IAsignacionMaterialesService entregasService,
            IEmpleadoService empleadoService,
            IContratistaService contratistaService,
            IProyectoService proyectoService,
            IReporteService reporteService,
            IReportePdfService reportePdfService,
            ILogger<AsignacionesPersonalController> logger)
        {
            _equipoService = equipoService;
            _entregasService = entregasService;
            _empleadoService = empleadoService;
            _contratistaService = contratistaService;
            _proyectoService = proyectoService;
            _reporteService = reporteService;
            _reportePdfService = reportePdfService;
            _logger = logger;
        }

        // Indices de navegación: se conservan las acciones operativas originales.
        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> Index(AsignacionesPersonalFiltroDto? filtro = null)
        {
            filtro ??= new AsignacionesPersonalFiltroDto();
            var asignaciones = await _equipoService.GetAllAsignacionesAsync(filtro);
            ViewBag.Proyectos = await _proyectoService.GetProyectosAsync();
            ViewBag.Filtro = filtro;
            return View("~/Views/AsignacionesPersonal/Index.cshtml", asignaciones);
        }

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public IActionResult IndexEntregasMateriales() => RedirectToAction("RegistrarEntrega", "AsignacionMateriales");

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public IActionResult IndexSobrantes() => RedirectToAction("Index", "MaterialesSobrantes");

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public IActionResult IndexAsignacionesPersonal() => RedirectToAction(nameof(Index));

        // ================================================================
        // INDEX - PANEL PRINCIPAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> IndexEquipo()
        {
            try
            {
                var model = await _equipoService.GetEstadisticasAsignacionesPersonalAsync();

                var equipoReciente = await _equipoService.GetAllAsignacionesAsync(
                    new AsignacionesPersonalFiltroDto
                    {
                        SoloActivos = true,
                        Page = 1,
                        PageSize = 5
                    });

                ViewBag.EquipoReciente = equipoReciente.ToList();
                ViewBag.EquipoRecienteCount = equipoReciente.Count();

                // Las entregas y sobrantes pertenecen al módulo Logística.
                // Mantener estas claves vacías mientras se actualiza el dashboard Razor.
                ViewBag.AsignacionesRecientes = new List<AsignacionMaterialesMaterialDto>();
                ViewBag.AsignacionesRecientesCount = 0;

                var todasPersonas =
                    await _equipoService.GetAllPersonasAsync(
                        new AsignacionesPersonalFiltroDto
                        {
                            SoloActivos = true
                        });

                var personasSinProyecto =
                    todasPersonas
                        .Where(p => !p.IdProyectoActivo.HasValue)
                        .Take(10)
                        .ToList();

                ViewBag.PersonasSinProyecto = personasSinProyecto;
                ViewBag.PersonasSinProyectoCount = personasSinProyecto.Count();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar el panel principal de Asignaciones de personal");

                TempData["MensajeError"] =
                    "Error al cargar el panel principal";

                var modelVacio = new AsignacionesPersonalEstadisticasDto
                {
                    Total = 0,
                    TotalEmpleados = 0,
                    TotalContratistas = 0,
                    Activos = 0,
                    Inactivos = 0,
                    EnProyectos = 0,
                    SinProyectos = 0,
                    NuevosMes = 0,
                    PorEspecialidad = new Dictionary<string, int>(),
                    PorTipo = new Dictionary<string, int>(),
                    UltimoRegistro = null,
                    CalificacionPromedio = 0
                };

                ViewBag.EquipoReciente =
                    new List<AsignacionPersonalProyectoListDto>();

                ViewBag.EquipoRecienteCount = 0;

                ViewBag.AsignacionesRecientes =
                    new List<AsignacionMaterialesMaterialDto>();

                ViewBag.AsignacionesRecientesCount = 0;

                ViewBag.PersonasSinProyecto =
                    new List<AsignacionesPersonalListDto>();

                ViewBag.PersonasSinProyectoCount = 0;

                return View(modelVacio);
            }
        }

        // ================================================================
        // PERSONAS (EMPLEADOS + CONTRATISTAS) - LISTADO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> Personas(
            AsignacionesPersonalFiltroDto? filtro = null)
        {
            try
            {
                filtro ??= new AsignacionesPersonalFiltroDto();

                var personas =
                    await _equipoService.GetAllPersonasAsync(filtro);

                ViewBag.Filtro = filtro;

                return View(personas);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar personas");

                TempData["MensajeError"] =
                    "Error al cargar las personas";

                return View(
                    new List<AsignacionesPersonalListDto>());
            }
        }

        // ================================================================
        // PERSONA - DETALLES
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> DetallesPersona(
            int id,
            string tipo)
        {
            try
            {
                var persona =
                    await _equipoService.GetPersonaByIdAsync(
                        id,
                        tipo);

                if (persona == null)
                {
                    TempData["MensajeError"] =
                        "Persona no encontrada";

                    return RedirectToAction(
                        "Personas");
                }

                return View(persona);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar detalles de persona");

                TempData["MensajeError"] =
                    "Error al cargar los detalles";

                return RedirectToAction(
                    "Personas");
            }
        }

        // ================================================================
        // TOGGLE ACTIVO - EMPLEADO
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("EQUIPO.EDITAR")]
        public async Task<IActionResult> ToggleActivoEmpleado(int id)
        {
            try
            {
                var result =
                    await _empleadoService.ToggleActivoAsync(id);

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;
                }
                else
                {
                    TempData["MensajeError"] =
                        result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar estado del empleado {Id}",
                    id);

                TempData["MensajeError"] =
                    "Error al cambiar el estado del empleado";
            }

            return RedirectToAction(
                "Personas");
        }

        // ================================================================
        // TOGGLE ACTIVO - CONTRATISTA
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("EQUIPO.EDITAR")]
        public async Task<IActionResult> ToggleActivoContratista(int id)
        {
            try
            {
                var result =
                    await _contratistaService.ToggleActivoAsync(id);

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;
                }
                else
                {
                    TempData["MensajeError"] =
                        result.Message;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar estado del contratista {Id}",
                    id);

                TempData["MensajeError"] =
                    "Error al cambiar el estado del contratista";
            }

            return RedirectToAction(
                "Personas");
        }

        // ================================================================
        // EXPORTAR PERSONAS A EXCEL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> ExportarPersonasExcel(
            AsignacionesPersonalFiltroDto? filtro = null)
        {
            try
            {
                filtro ??= new AsignacionesPersonalFiltroDto();

                var personas =
                    await _equipoService.GetAllPersonasAsync(filtro);

                var data =
                    personas.Select(p => new
                    {
                        p.Tipo,
                        p.NombreCompleto,
                        p.NumeroDocumento,
                        p.Especialidad,
                        p.Telefono,
                        p.CorreoElectronico,
                        ProyectoActual =
                            p.CodigoProyectoActivo
                            ?? "Sin proyecto",
                        Estado =
                            p.Activo
                                ? "Activo"
                                : "Inactivo"
                    });

                var columnas = new[]
                {
                    "Tipo",
                    "Nombre",
                    "Documento",
                    "Especialidad",
                    "Telefono",
                    "Correo",
                    "Proyecto Actual",
                    "Estado"
                };

                var bytes =
                    await _reporteService.ExportarExcelAsync(
                        data,
                        "Equipo_Vidal",
                        columnas);

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Equipo_Vidal_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al exportar personas a Excel");

                TempData["MensajeError"] =
                    "Error al exportar";

                return RedirectToAction(
                    "Personas");
            }
        }

        // ================================================================
        // EXPORTAR PERSONAS A PDF
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> ExportarPersonasPdf(
            AsignacionesPersonalFiltroDto? filtro = null)
        {
            try
            {
                filtro ??=
                    new AsignacionesPersonalFiltroDto();

                var personas =
                    await _equipoService.GetAllPersonasAsync(
                        filtro);

                var bytes =
                    await _reportePdfService
                        .GenerarPdfEquipoAsync(personas);

                return File(
                    bytes,
                    "application/pdf",
                    $"Equipo_Vidal_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al exportar personas a PDF");

                TempData["MensajeError"] =
                    "Error al exportar";

                return RedirectToAction(
                    "Personas");
            }
        }

        // ================================================================
        // BUSCAR PERSONA POR DOCUMENTO (AJAX)
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> BuscarPersona(
            string documento)
        {
            try
            {
                if (string.IsNullOrEmpty(documento))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Documento requerido"
                    });
                }

                var persona =
                    await _equipoService
                        .GetPersonaByDocumentoAsync(documento);

                if (persona == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Persona no encontrada"
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        persona.Tipo,
                        persona.Id,
                        persona.NombreCompleto,
                        persona.NumeroDocumento,
                        persona.Especialidad,
                        persona.Telefono,
                        persona.CorreoElectronico,
                        persona.IdProyectoActivo,
                        persona.CodigoProyectoActivo,
                        persona.DescripcionProyectoActivo,
                        persona.RolEnProyectoActivo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al buscar persona");

                return Json(new
                {
                    success = false,
                    message = "Error al buscar persona"
                });
            }
        }

        // ================================================================
        // LISTAR PERSONAL ACTIVO POR TIPO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> GetPersonalPorTipo(
            string tipo)
        {
            try
            {
                if (tipo != "Empleado" &&
                    tipo != "Contratista")
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Tipo inválido. Use 'Empleado' o 'Contratista'."
                    });
                }

                var filtro =
                    new AsignacionesPersonalFiltroDto
                    {
                        Tipo = tipo,
                        SoloActivos = true,
                        Page = 1,
                        PageSize = 1000
                    };

                var lista =
                    await _equipoService
                        .GetAllPersonasAsync(filtro);

                var data =
                    lista
                        .OrderBy(p => p.NombreCompleto)
                        .Select(p => new
                        {
                            p.Id,
                            p.NombreCompleto,
                            p.NumeroDocumento,
                            p.Especialidad,
                            p.IdProyectoActivo,
                            p.CodigoProyectoActivo
                        });

                return Json(new
                {
                    success = true,
                    data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al listar personal por tipo");

                return Json(new
                {
                    success = false,
                    message =
                        "Error al obtener el personal"
                });
            }
        }

        // ================================================================
        // ASIGNACIÓN DE PERSONAL A PROYECTOS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.EDITAR")]
        public async Task<IActionResult> AsignarPersonal(
            int? idProyecto = null)
        {
            try
            {
                ViewBag.Proyectos =
                    await _proyectoService
                        .GetProyectosAsync();

                ViewBag.Empleados =
                    await _empleadoService
                        .GetEmpleadosActivosAsync();

                ViewBag.Contratistas =
                    await _contratistaService
                        .GetContratistasActivosAsync();

                var model =
                    new AsignarPersonalProyectoDto();

                if (idProyecto.HasValue)
                {
                    model.IdProyecto =
                        idProyecto.Value;
                }

                return View("~/Views/AsignacionesPersonal/AsignarPersonal.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar formulario de asignación de personal");

                TempData["MensajeError"] =
                    "Error al cargar el formulario";

                return RedirectToAction(
                    "Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("EQUIPO.EDITAR")]
        public async Task<IActionResult> AsignarPersonal(
            AsignarPersonalProyectoDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Proyectos =
                    await _proyectoService
                        .GetProyectosAsync();

                ViewBag.Empleados =
                    await _empleadoService
                        .GetEmpleadosActivosAsync();

                ViewBag.Contratistas =
                    await _contratistaService
                        .GetContratistasActivosAsync();

                return View("~/Views/AsignacionesPersonal/AsignarPersonal.cshtml", model);
            }

            try
            {
                var usuarioId =
                    HttpContext.Session.GetInt32(
                        "IdUsuario") ?? 0;

                var result =
                    await _equipoService
                        .AsignarAProyectoAsync(
                            model,
                            usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;

                    return RedirectToAction(
                        "AsignacionesPersonal");
                }

                TempData["MensajeError"] =
                    result.Message;

                ViewBag.Proyectos =
                    await _proyectoService
                        .GetProyectosAsync();

                ViewBag.Empleados =
                    await _empleadoService
                        .GetEmpleadosActivosAsync();

                ViewBag.Contratistas =
                    await _contratistaService
                        .GetContratistasActivosAsync();

                return View("~/Views/AsignacionesPersonal/AsignarPersonal.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al asignar personal");

                TempData["MensajeError"] =
                    "Error al asignar personal";

                ViewBag.Proyectos =
                    await _proyectoService
                        .GetProyectosAsync();

                ViewBag.Empleados =
                    await _empleadoService
                        .GetEmpleadosActivosAsync();

                ViewBag.Contratistas =
                    await _contratistaService
                        .GetContratistasActivosAsync();

                return View("~/Views/AsignacionesPersonal/AsignarPersonal.cshtml", model);
            }
        }

        // ================================================================
        // ASIGNACIONES DE PERSONAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> AsignacionesPersonal(
            AsignacionesPersonalFiltroDto? filtro = null)
        {
            try
            {
                filtro ??=
                    new AsignacionesPersonalFiltroDto();

                var asignaciones =
                    await _equipoService
                        .GetAllAsignacionesAsync(filtro);

                ViewBag.Proyectos =
                    await _proyectoService
                        .GetProyectosAsync();

                ViewBag.Filtro =
                    filtro;

                return View("~/Views/AsignacionesPersonal/AsignacionesPersonal.cshtml", asignaciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar asignaciones de personal");

                TempData["MensajeError"] =
                    "Error al cargar las asignaciones";

                return View("~/Views/AsignacionesPersonal/AsignacionesPersonal.cshtml",
                    new List<AsignacionPersonalProyectoListDto>());
            }
        }

        // ================================================================
        // EXPORTAR ASIGNACIONES PERSONAL A EXCEL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> ExportarAsignacionesPersonalExcel(
            string? buscar = null,
            int? idProyecto = null,
            string? tipo = null,
            bool? soloActivos = null)
        {
            try
            {
                var filtro =
                    new AsignacionesPersonalFiltroDto
                    {
                        Buscar = buscar,
                        IdProyecto = idProyecto,
                        Tipo = tipo,
                        SoloActivos = soloActivos
                    };

                var asignaciones =
                    await _equipoService
                        .GetAllAsignacionesAsync(filtro);

                var data =
                    asignaciones.Select(a => new
                    {
                        a.Tipo,
                        a.NombreCompleto,
                        a.NumeroDocumento,
                        a.CodigoProyecto,
                        a.RolEnProyecto,
                        FechaInicio =
                            a.FechaHoraInicio
                                .ToString(
                                    "dd/MM/yyyy HH:mm"),
                        FechaFin =
                            a.FechaHoraFin?
                                .ToString(
                                    "dd/MM/yyyy HH:mm")
                            ?? "En curso",
                        a.TiempoAsignado,
                        Estado =
                            a.Activo
                                ? "Activo"
                                : "Finalizado"
                    });

                var columnas = new[]
                {
                    "Tipo",
                    "Persona",
                    "Documento",
                    "Proyecto",
                    "Rol",
                    "Fecha Inicio",
                    "Fecha Fin",
                    "Tiempo",
                    "Estado"
                };

                var bytes =
                    await _reporteService
                        .ExportarExcelAsync(
                            data,
                            "Asignaciones_Personal",
                            columnas);

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Asignaciones_Personal_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al exportar asignaciones de personal a Excel");

                TempData["MensajeError"] =
                    "Error al exportar";

                return RedirectToAction(
                    "AsignacionesPersonal");
            }
        }

        // ================================================================
        // EXPORTAR ASIGNACIONES PERSONAL A PDF
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> ExportarAsignacionesPersonalPdf(
            string? buscar = null,
            int? idProyecto = null,
            string? tipo = null,
            bool? soloActivos = null)
        {
            try
            {
                var filtro =
                    new AsignacionesPersonalFiltroDto
                    {
                        Buscar = buscar,
                        IdProyecto = idProyecto,
                        Tipo = tipo,
                        SoloActivos = soloActivos
                    };

                var asignaciones =
                    await _equipoService
                        .GetAllAsignacionesAsync(filtro);

                var bytes =
                    await _reportePdfService
                        .GenerarPdfAsignacionesPersonalAsync(
                            asignaciones);

                return File(
                    bytes,
                    "application/pdf",
                    $"Asignaciones_Personal_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al exportar asignaciones de personal a PDF");

                TempData["MensajeError"] =
                    "Error al exportar";

                return RedirectToAction(
                    "AsignacionesPersonal");
            }
        }

        // ================================================================
        // DETALLES DE ASIGNACIÓN DE PERSONAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> DetallesAsignacionPersonal(
            int id)
        {
            try
            {
                var asignacion =
                    await _equipoService
                        .GetAsignacionByIdAsync(id);

                if (asignacion == null)
                {
                    TempData["MensajeError"] =
                        "Asignación no encontrada";

                    return RedirectToAction(
                        "AsignacionesPersonal");
                }

                return View("~/Views/AsignacionesPersonal/DetallesAsignacionPersonal.cshtml", asignacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar detalles de asignación");

                TempData["MensajeError"] =
                    "Error al cargar los detalles";

                return RedirectToAction(
                    "AsignacionesPersonal");
            }
        }

        // ================================================================
        // FINALIZAR ASIGNACIÓN DE PERSONAL
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("EQUIPO.EDITAR")]
        public async Task<IActionResult> FinalizarAsignacionPersonal(
            int id,
            string? motivo = null)
        {
            try
            {
                var result =
                    await _equipoService
                        .FinalizarAsignacionAsync(
                            id,
                            motivo);

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;
                }
                else
                {
                    TempData["MensajeError"] =
                        result.Message;
                }

                return RedirectToAction(
                    "AsignacionesPersonal");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al finalizar asignación de personal");

                TempData["MensajeError"] =
                    "Error al finalizar la asignación";

                return RedirectToAction(
                    "AsignacionesPersonal");
            }
        }

        // ================================================================
        // ASIGNACIONES POR PROYECTO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> AsignacionesByProyecto(
            int idProyecto)
        {
            try
            {
                var proyecto =
                    await _proyectoService
                        .GetProyectoByIdAsync(idProyecto);

                if (proyecto == null)
                {
                    TempData["MensajeError"] =
                        "Proyecto no encontrado";

                    return RedirectToAction(
                        "Index");
                }

                var personal =
                    await _equipoService
                        .GetPersonalActivoByProyectoAsync(
                            idProyecto);

                // Entregas efectivas del proyecto. Las devoluciones se registran aparte
                // en Sobrantes; no se infieren saldos de devolución desde una entrega.
                var entregas = await _entregasService.GetEntregasByProyectoAsync(idProyecto);
                var materiales = entregas.SelectMany(entrega => entrega.Materiales.Select(detalle => new AsignacionMaterialesMaterialDto
                {
                    IdAsignacion = detalle.IdAsignacion,
                    IdProyecto = entrega.IdProyecto,
                    IdMaterial = detalle.IdMaterial,
                    Material = detalle.Material,
                    CantidadSolicitada = detalle.CantidadSolicitadaUnidadBase,
                    CantidadEntregada = detalle.CantidadEntregadaUnidadBase,
                    Estado = detalle.EstadoEntrega,
                    FechaAsignacion = entrega.FechaEntrega,
                    IdPersona = entrega.IdEmpleado ?? entrega.IdContratista ?? 0,
                    Tipo = entrega.IdEmpleado.HasValue ? "Empleado" : "Contratista",
                    NombreCompleto = personal.FirstOrDefault(p => p.IdMiembro == (entrega.IdEmpleado ?? entrega.IdContratista) && p.Tipo == (entrega.IdEmpleado.HasValue ? "Empleado" : "Contratista"))?.NombreCompleto ?? "Personal no disponible",
                    CostoUnitarioRef = detalle.CostoUnitarioRef
                })).ToList();

                ViewBag.Proyecto =
                    proyecto;

                ViewBag.Personal =
                    personal;

                ViewBag.Materiales =
                    materiales;

                return View("~/Views/AsignacionesPersonal/AsignacionesByProyecto.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar asignaciones por proyecto");

                TempData["MensajeError"] =
                    "Error al cargar las asignaciones";

                return RedirectToAction(
                    "Index");
            }
        }

        // ================================================================
        // PERSONAL ACTIVO POR PROYECTO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> GetPersonalActivoByProyecto(
            int idProyecto)
        {
            try
            {
                var personal =
                    await _equipoService
                        .GetPersonalActivoByProyectoAsync(
                            idProyecto);

                return Json(new
                {
                    success = true,
                    data =
                        personal.Select(p => new
                        {
                            p.IdAsignacion,
                            p.Tipo,
                            p.NombreCompleto,
                            p.NumeroDocumento,
                            p.RolEnProyecto,
                            p.FechaInicioFormateada,
                            p.TiempoAsignado,
                            p.EstadoBadge
                        })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener personal activo del proyecto");

                return Json(new
                {
                    success = false,
                    message =
                        "Error al cargar personal"
                });
            }
        }

        // ================================================================
        // ESTADÍSTICAS DEL EQUIPO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("EQUIPO.VER")]
        public async Task<IActionResult> GetEstadisticasEquipo()
        {
            try
            {
                var estadisticas =
                    await _equipoService
                        .GetEstadisticasAsignacionesAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        estadisticas.TotalAsignaciones,
                        estadisticas.AsignacionesActivas,
                        estadisticas.AsignacionesFinalizadas,
                        estadisticas.TotalEmpleadosAsignados,
                        estadisticas.TotalContratistasAsignados,
                        estadisticas.ProyectosConEquipo,
                        estadisticas.ProyectosSinEquipo,
                        estadisticas.AsignacionesPorTipo,
                        estadisticas.UltimaAsignacion,
                        estadisticas.PromedioMiembrosPorProyecto
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener estadísticas del equipo");

                return Json(new
                {
                    success = false,
                    message =
                        "Error al obtener estadísticas"
                });
            }
        }
    }
}
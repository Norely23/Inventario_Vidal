using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    public class ProyectosController : Controller
    {
        private readonly IProyectoService _proyectoService;
        private readonly IClienteService _clienteService;
        private readonly IMaterialService _materialService;
        private readonly ILogger<ProyectosController> _logger;

        public ProyectosController(
            IProyectoService proyectoService,
            IClienteService clienteService,
            IMaterialService materialService,
            ILogger<ProyectosController> logger)
        {
            _proyectoService = proyectoService;
            _clienteService = clienteService;
            _materialService = materialService;
            _logger = logger;
        }

        // ================================================================
        // INDEX - LISTADO DE PROYECTOS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var proyectos = await _proyectoService.GetProyectosAsync();
                var estados = await _proyectoService.GetEstadosProyectoAsync();
                var tipos = await _proyectoService.GetTiposProyectoAsync();

                ViewBag.Estados = estados;
                ViewBag.Tipos = tipos;

                return View(proyectos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar proyectos");
                TempData["MensajeError"] = "Error al cargar los proyectos";
                return View(new List<ProyectoListDto>());
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.CREAR")]
        public async Task<IActionResult> Crear()
        {
            try
            {
                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();

                return View(new ProyectoCreateDto
                {
                    FechaInicio = DateOnly.FromDateTime(DateTime.Now),
                    Moneda = "PEN",
                    Prioridad = "Media"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de creación de proyecto");
                TempData["MensajeError"] = "Error al cargar el formulario";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.CREAR")]
        public async Task<IActionResult> Crear(ProyectoCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();
                return View(model);
            }

            try
            {
                var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
                var result = await _proyectoService.CreateProyectoAsync(model, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Detalles", new { id = result.IdProyecto });
                }

                TempData["MensajeError"] = result.Message;
                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proyecto");
                TempData["MensajeError"] = "Error al crear el proyecto";
                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();
                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var proyecto = await _proyectoService.GetProyectoByIdAsync(id);
                if (proyecto == null)
                {
                    TempData["MensajeError"] = "Proyecto no encontrado";
                    return RedirectToAction("Index");
                }

                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();
                ViewBag.Estados = await _proyectoService.GetEstadosProyectoAsync();

                var model = new ProyectoEditDto
                {
                    IdProyecto = proyecto.IdProyecto,
                    IdCliente = proyecto.IdCliente,
                    IdTipoProyecto = proyecto.IdTipoProyecto,
                    IdEstado = proyecto.IdEstado,
                    Descripcion = proyecto.Descripcion,
                    FechaInicio = proyecto.FechaInicio,
                    FechaEntregaEstimada = proyecto.FechaEntregaEstimada,
                    FechaEntregaReal = proyecto.FechaEntregaReal,
                    PrecioVenta = proyecto.PrecioVenta,
                    CostoEstimado = proyecto.CostoEstimado,
                    Moneda = proyecto.Moneda,
                    Observaciones = proyecto.Observaciones,
                    Prioridad = proyecto.Prioridad
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar proyecto para editar");
                TempData["MensajeError"] = "Error al cargar el proyecto";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> Editar(ProyectoEditDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();
                ViewBag.Estados = await _proyectoService.GetEstadosProyectoAsync();
                return View(model);
            }

            try
            {
                var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
                var result = await _proyectoService.UpdateProyectoAsync(model, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction("Detalles", new { id = model.IdProyecto });
                }

                TempData["MensajeError"] = result.Message;
                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();
                ViewBag.Estados = await _proyectoService.GetEstadosProyectoAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar proyecto");
                TempData["MensajeError"] = "Error al actualizar el proyecto";
                ViewBag.Clientes = await _clienteService.GetClientesActivosAsync();
                ViewBag.TiposProyecto = await _proyectoService.GetTiposProyectoAsync();
                ViewBag.Estados = await _proyectoService.GetEstadosProyectoAsync();
                return View(model);
            }
        }

        // ================================================================
        // DETALLES - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var proyecto = await _proyectoService.GetProyectoByIdAsync(id);
                if (proyecto == null)
                {
                    TempData["MensajeError"] = "Proyecto no encontrado";
                    return RedirectToAction("Index");
                }

                var estados = await _proyectoService.GetEstadosProyectoAsync();
                var materiales = await _materialService.GetMaterialesActivosAsync();

                ViewBag.Estados = estados;
                ViewBag.Materiales = materiales;

                return View(proyecto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del proyecto");
                TempData["MensajeError"] = "Error al cargar los detalles";
                return RedirectToAction("Index");
            }
        }

        // ================================================================
        // CAMBIAR ESTADO - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> CambiarEstado(int idProyecto, int idEstado)
        {
            try
            {
                var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
                var result = await _proyectoService.CambiarEstadoAsync(idProyecto, idEstado, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction("Detalles", new { id = idProyecto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del proyecto {IdProyecto}", idProyecto);
                TempData["MensajeError"] = "Error al cambiar el estado del proyecto";
                return RedirectToAction("Detalles", new { id = idProyecto });
            }
        }

        // ================================================================
        // ENTREGA FINAL DEL PROYECTO AL CLIENTE - POST
        // Formulario integrado en Views/Proyectos/Detalles.cshtml.
        // No corresponde a las salidas de materiales del almacén.
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> Entregar(int idProyecto, DateTime fechaEntregaReal,
            string? observacionesEntrega, bool confirmarEntrega)
        {
            if (idProyecto <= 0)
            {
                TempData["MensajeError"] = "El proyecto indicado no es válido.";
                return RedirectToAction(nameof(Index));
            }

            if (!confirmarEntrega)
            {
                TempData["MensajeError"] = "Debes confirmar la entrega efectiva al cliente.";
                return RedirectToAction(nameof(Detalles), new { id = idProyecto });
            }

            if (fechaEntregaReal == default || fechaEntregaReal.Date > DateTime.Today)
            {
                TempData["MensajeError"] = "Indica una fecha de entrega válida que no sea futura.";
                return RedirectToAction(nameof(Detalles), new { id = idProyecto });
            }

            if (observacionesEntrega?.Length > 500)
            {
                TempData["MensajeError"] = "Las observaciones no pueden superar 500 caracteres.";
                return RedirectToAction(nameof(Detalles), new { id = idProyecto });
            }

            try
            {
                var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
                var result = await _proyectoService.EntregarProyectoAsync(
                    idProyecto, fechaEntregaReal, observacionesEntrega?.Trim(), usuarioId);

                if (result.Success)
                    TempData["MensajeExito"] = result.Message;
                else
                    TempData["MensajeError"] = result.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar la entrega final del proyecto {IdProyecto}", idProyecto);
                TempData["MensajeError"] = "No se pudo confirmar la entrega final del proyecto.";
            }

            return RedirectToAction(nameof(Detalles), new { id = idProyecto });
        }

        // ================================================================
        // ⚠️ MÉTODOS ELIMINADOS (AHORA EN AsignacionesPersonalController)
        // ================================================================

        // ❌ AsignarMaterial - POST (ELIMINADO)
        // ❌ DevolverMaterial - POST (ELIMINADO)

        // Estos métodos ahora se manejan en AsignacionesPersonalController:
        // - AsignarMaterial → AsignacionesPersonalController.AsignarMaterial
        // - DevolverMaterial → AsignacionesPersonalController.RegistrarUsoDevolucion

        // ================================================================
        // REGISTRAR COSTO - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> RegistrarCosto(CostoProyectoDto model)
        {
            try
            {
                if (model.Monto <= 0)
                {
                    TempData["MensajeError"] = "El monto debe ser mayor a 0";
                    return RedirectToAction("Detalles", new { id = model.IdProyecto });
                }

                var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
                var result = await _proyectoService.RegistrarCostoAsync(model, usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction("Detalles", new { id = model.IdProyecto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar costo en el proyecto");
                TempData["MensajeError"] = "Error al registrar el costo";
                return RedirectToAction("Detalles", new { id = model.IdProyecto });
            }
        }

        // ================================================================
        // SUBIR PLANO - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PLANO.GESTIONAR")]
        public async Task<IActionResult> SubirPlano(int idProyecto, IFormFile? archivo, string? observaciones)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                {
                    TempData["MensajeError"] = "Debe seleccionar un archivo";
                    return RedirectToAction("Detalles", new { id = idProyecto });
                }

                var extension = Path.GetExtension(archivo.FileName).ToLower();
                if (extension != ".pdf")
                {
                    TempData["MensajeError"] = "Solo se permiten archivos PDF";
                    return RedirectToAction("Detalles", new { id = idProyecto });
                }

                if (archivo.Length > 10 * 1024 * 1024)
                {
                    TempData["MensajeError"] = "El archivo no debe exceder los 10MB";
                    return RedirectToAction("Detalles", new { id = idProyecto });
                }

                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "planos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var nombreArchivo = $"{idProyecto}_{DateTime.Now:yyyyMMddHHmmss}_{archivo.FileName}";
                var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                var rutaArchivo = $"/uploads/planos/{nombreArchivo}";
                var usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;

                var result = await _proyectoService.SubirPlanoAsync(idProyecto, archivo.FileName, rutaArchivo, usuarioId, observaciones);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction("Detalles", new { id = idProyecto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir plano al proyecto {IdProyecto}", idProyecto);
                TempData["MensajeError"] = "Error al subir el plano";
                return RedirectToAction("Detalles", new { id = idProyecto });
            }
        }

        // ================================================================
        // ESTADOS DE PROYECTO - INDEX
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> Estados()
        {
            try
            {
                var estados = await _proyectoService.GetEstadosProyectoAsync();
                return View("Estados/Index", estados);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar estados de proyecto");
                TempData["MensajeError"] = "Error al cargar los estados";
                return View("Estados/Index", new List<EstadoProyectoDto>());
            }
        }

        // ================================================================
        // ESTADOS DE PROYECTO - MANTENIMIENTO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.CREAR")]
        public IActionResult CrearEstado()
        {
            return View("Estados/Crear", new EstadoProyectoDto
            {
                Orden = 1,
                ColorHex = "#0d6efd"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.CREAR")]
        public async Task<IActionResult> CrearEstado(EstadoProyectoDto modelo)
        {
            ValidarEstadoProyecto(modelo);
            if (!ModelState.IsValid)
                return View("Estados/Crear", modelo);

            try
            {
                modelo.Nombre = modelo.Nombre.Trim();
                var resultado = await _proyectoService.CreateEstadoProyectoAsync(modelo);
                if (resultado.Success)
                {
                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(Estados));
                }

                ModelState.AddModelError(string.Empty, resultado.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear estado de proyecto");
                ModelState.AddModelError(string.Empty, "No fue posible registrar el estado.");
            }

            return View("Estados/Crear", modelo);
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> EditarEstado(int id)
        {
            if (id <= 0) return BadRequest();
            var estado = await _proyectoService.GetEstadoProyectoByIdAsync(id);
            if (estado == null)
            {
                TempData["MensajeError"] = "El estado solicitado no existe.";
                return RedirectToAction(nameof(Estados));
            }
            return View("Estados/Editar", estado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> EditarEstado(EstadoProyectoDto modelo)
        {
            if (modelo.IdEstado <= 0)
                ModelState.AddModelError(nameof(modelo.IdEstado), "Identificador de estado inválido.");
            ValidarEstadoProyecto(modelo);
            if (!ModelState.IsValid)
                return View("Estados/Editar", modelo);

            try
            {
                modelo.Nombre = modelo.Nombre.Trim();
                var resultado = await _proyectoService.UpdateEstadoProyectoAsync(modelo);
                if (resultado.Success)
                {
                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(Estados));
                }
                ModelState.AddModelError(string.Empty, resultado.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar estado de proyecto {IdEstado}", modelo.IdEstado);
                ModelState.AddModelError(string.Empty, "No fue posible actualizar el estado.");
            }
            return View("Estados/Editar", modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> EliminarEstado(int id)
        {
            if (id <= 0)
            {
                TempData["MensajeError"] = "Identificador de estado inválido.";
                return RedirectToAction(nameof(Estados));
            }

            try
            {
                // El servicio impide eliminar estados asociados a proyectos.
                var resultado = await _proyectoService.DeleteEstadoProyectoAsync(id);
                TempData[resultado.Success ? "MensajeExito" : "MensajeError"] = resultado.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar estado de proyecto {IdEstado}", id);
                TempData["MensajeError"] = "No fue posible eliminar el estado.";
            }
            return RedirectToAction(nameof(Estados));
        }

        private void ValidarEstadoProyecto(EstadoProyectoDto modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
                ModelState.AddModelError(nameof(modelo.Nombre), "El nombre del estado es obligatorio.");
            else if (modelo.Nombre.Trim().Length > 100)
                ModelState.AddModelError(nameof(modelo.Nombre), "El nombre no debe superar 100 caracteres.");

            if (modelo.Orden < 1)
                ModelState.AddModelError(nameof(modelo.Orden), "El orden debe ser mayor o igual a uno.");

            if (!string.IsNullOrWhiteSpace(modelo.ColorHex) &&
                !System.Text.RegularExpressions.Regex.IsMatch(modelo.ColorHex,
                    @"^#[0-9a-fA-F]{6}$"))
                ModelState.AddModelError(nameof(modelo.ColorHex), "Utiliza un color hexadecimal válido (#RRGGBB).");
        }

        // ================================================================
        // TIPOS DE PROYECTO - INDEX
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> Tipos()
        {
            try
            {
                var tipos = await _proyectoService.GetTiposProyectoAsync();
                return View("Tipos/Index", tipos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar tipos de proyecto");
                TempData["MensajeError"] = "Error al cargar los tipos";
                return View("Tipos/Index", new List<TipoProyectoDto>());
            }
        }

        // ================================================================
        // TIPOS DE PROYECTO - MANTENIMIENTO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.CREAR")]
        public IActionResult CrearTipo()
        {
            return View("Tipos/Crear", new TipoProyectoDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.CREAR")]
        public async Task<IActionResult> CrearTipo(TipoProyectoDto modelo)
        {
            ValidarTipoProyecto(modelo);
            if (!ModelState.IsValid)
                return View("Tipos/Crear", modelo);

            try
            {
                modelo.Nombre = modelo.Nombre.Trim();
                modelo.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion)
                    ? null : modelo.Descripcion.Trim();
                var resultado = await _proyectoService.CreateTipoProyectoAsync(modelo);
                if (resultado.Success)
                {
                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(Tipos));
                }
                ModelState.AddModelError(string.Empty, resultado.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tipo de proyecto");
                ModelState.AddModelError(string.Empty, "No fue posible registrar el tipo.");
            }
            return View("Tipos/Crear", modelo);
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> EditarTipo(int id)
        {
            if (id <= 0) return BadRequest();
            try
            {
                var tipo = await _proyectoService.GetTipoProyectoByIdAsync(id);
                if (tipo == null)
                {
                    TempData["MensajeError"] = "El tipo de proyecto solicitado no existe.";
                    return RedirectToAction(nameof(Tipos));
                }
                return View("Tipos/Editar", tipo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar tipo de proyecto {IdTipo}", id);
                TempData["MensajeError"] = "No fue posible consultar el tipo de proyecto.";
                return RedirectToAction(nameof(Tipos));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> EditarTipo(TipoProyectoDto modelo)
        {
            if (modelo.IdTipoProyecto <= 0)
                ModelState.AddModelError(nameof(modelo.IdTipoProyecto), "Identificador de tipo inválido.");
            ValidarTipoProyecto(modelo);
            if (!ModelState.IsValid)
                return View("Tipos/Editar", modelo);

            try
            {
                modelo.Nombre = modelo.Nombre.Trim();
                modelo.Descripcion = string.IsNullOrWhiteSpace(modelo.Descripcion)
                    ? null : modelo.Descripcion.Trim();
                var resultado = await _proyectoService.UpdateTipoProyectoAsync(modelo);
                if (resultado.Success)
                {
                    TempData["MensajeExito"] = resultado.Message;
                    return RedirectToAction(nameof(Tipos));
                }
                ModelState.AddModelError(string.Empty, resultado.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar tipo de proyecto {IdTipo}", modelo.IdTipoProyecto);
                ModelState.AddModelError(string.Empty, "No fue posible actualizar el tipo.");
            }
            return View("Tipos/Editar", modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROYECTO.EDITAR")]
        public async Task<IActionResult> EliminarTipo(int id)
        {
            if (id <= 0)
            {
                TempData["MensajeError"] = "Identificador de tipo inválido.";
                return RedirectToAction(nameof(Tipos));
            }
            try
            {
                // El servicio impide eliminar tipos utilizados por algún proyecto.
                var resultado = await _proyectoService.DeleteTipoProyectoAsync(id);
                TempData[resultado.Success ? "MensajeExito" : "MensajeError"] = resultado.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar tipo de proyecto {IdTipo}", id);
                TempData["MensajeError"] = "No fue posible eliminar el tipo.";
            }
            return RedirectToAction(nameof(Tipos));
        }

        private void ValidarTipoProyecto(TipoProyectoDto modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Nombre))
                ModelState.AddModelError(nameof(modelo.Nombre), "El nombre del tipo es obligatorio.");
            else if (modelo.Nombre.Trim().Length < 3 || modelo.Nombre.Trim().Length > 100)
                ModelState.AddModelError(nameof(modelo.Nombre), "El nombre debe tener entre 3 y 100 caracteres.");

            if (!string.IsNullOrWhiteSpace(modelo.Descripcion) && modelo.Descripcion.Trim().Length > 250)
                ModelState.AddModelError(nameof(modelo.Descripcion), "La descripción no debe superar 250 caracteres.");
        }

        // ================================================================
        // MÉTODOS AJAX
        // ================================================================

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> GetProyectosSelect(string? term = null)
        {
            try
            {
                var proyectos = await _proyectoService.GetProyectosAsync();

                if (!string.IsNullOrEmpty(term))
                {
                    term = term.ToLower();
                    proyectos = proyectos.Where(p =>
                        p.CodigoProyecto.ToLower().Contains(term) ||
                        p.Cliente.ToLower().Contains(term));
                }

                var result = proyectos.Select(p => new
                {
                    p.IdProyecto,
                    p.CodigoProyecto,
                    p.Cliente,
                    p.Estado,
                    display = $"{p.CodigoProyecto} - {p.Cliente}"
                }).Take(20);

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos para select");
                return Json(new { success = false, message = "Error al cargar proyectos" });
            }
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> GetProyectosByCliente(int idCliente)
        {
            try
            {
                var proyectos = await _proyectoService.GetProyectosByClienteAsync(idCliente);

                var result = proyectos.Select(p => new
                {
                    p.IdProyecto,
                    p.CodigoProyecto,
                    p.Estado,
                    p.FechaInicio,
                    display = $"{p.CodigoProyecto} - {p.Cliente}"
                });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos por cliente");
                return Json(new { success = false, message = "Error al cargar proyectos" });
            }
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> GetEstadosProyecto()
        {
            try
            {
                var estados = await _proyectoService.GetEstadosProyectoAsync();

                return Json(new
                {
                    success = true,
                    data = estados.Select(e => new
                    {
                        e.IdEstado,
                        e.Nombre,
                        e.Orden,
                        e.BadgeColor
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estados de proyecto");
                return Json(new { success = false, message = "Error al cargar estados" });
            }
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> GetTiposProyecto()
        {
            try
            {
                var tipos = await _proyectoService.GetTiposProyectoAsync();

                return Json(new
                {
                    success = true,
                    data = tipos.Select(t => new
                    {
                        t.IdTipoProyecto,
                        t.Nombre,
                        t.CantidadProyectos
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tipos de proyecto");
                return Json(new { success = false, message = "Error al cargar tipos" });
            }
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> GetProyectosActivos()
        {
            try
            {
                var proyectos = await _proyectoService.GetProyectosAsync();
                var activos = proyectos.Where(p =>
                    p.Estado == "En Proceso de Fabricacion" ||
                    p.Estado == "En Proceso de Acabado" ||
                    p.Estado == "Instalacion de Electricidad");

                return Json(new
                {
                    success = true,
                    total = activos.Count(),
                    data = activos.Select(p => new
                    {
                        p.IdProyecto,
                        p.CodigoProyecto,
                        p.Cliente,
                        p.Estado,
                        p.FechaInicio,
                        p.FechaEntregaEstimada
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos activos");
                return Json(new { success = false, message = "Error al cargar proyectos" });
            }
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> GetProyectosAtrasados()
        {
            try
            {
                var proyectos = await _proyectoService.GetProyectosAsync();
                var hoy = DateOnly.FromDateTime(DateTime.Now);

                var atrasados = proyectos.Where(p =>
                    p.FechaEntregaEstimada.HasValue &&
                    p.FechaEntregaEstimada.Value < hoy &&
                    p.Estado != "Entregado" &&
                    p.Estado != "Cancelado");

                return Json(new
                {
                    success = true,
                    total = atrasados.Count(),
                    data = atrasados.Select(p => new
                    {
                        p.IdProyecto,
                        p.CodigoProyecto,
                        p.Cliente,
                        p.Estado,
                        p.FechaEntregaEstimada,
                        DiasAtraso = (hoy.DayNumber - p.FechaEntregaEstimada.Value.DayNumber)
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proyectos atrasados");
                return Json(new { success = false, message = "Error al cargar proyectos" });
            }
        }

        [HttpGet]
        [PermisoRequerido("PROYECTO.VER")]
        public async Task<IActionResult> ObtenerCostoEstimado(int idProyecto)
        {
            try
            {
                var proyecto = await _proyectoService.GetProyectoByIdAsync(idProyecto);

                if (proyecto == null)
                {
                    return Json(new { success = false, message = "Proyecto no encontrado" });
                }

                return Json(new
                {
                    success = true,
                    costoEstimado = proyecto.CostoEstimado,
                    costoReal = proyecto.CostoReal ?? 0,
                    moneda = proyecto.Moneda
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener costo estimado del proyecto");
                return Json(new { success = false, message = "Error al obtener costo" });
            }
        }
    }
}
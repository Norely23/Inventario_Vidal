using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class ClientesController : Controller
    {
        private readonly IClienteService _clienteService;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(
            IClienteService clienteService,
            ILogger<ClientesController> logger)
        {
            _clienteService = clienteService;
            _logger = logger;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [PermisoRequerido("CLIENTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var clientes = await _clienteService.GetClientesAsync();
                return View(clientes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar clientes");
                TempData["MensajeError"] = "Error al cargar los clientes.";
                return View(new List<ClienteListDto>());
            }
        }

        // ============================================================
        // CREAR - GET
        // ============================================================

        [PermisoRequerido("CLIENTE.GESTIONAR")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View(new ClienteCreateDto
            {
                TipoCliente = "Juridico",
                TipoDocumento = "RUC",
                Activo = true
            });
        }

        // ============================================================
        // CREAR - POST
        // ============================================================

        [PermisoRequerido("CLIENTE.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ClienteCreateDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _clienteService.CreateClienteAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["MensajeError"] = result.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                TempData["MensajeError"] = "Error al crear el cliente.";
                return View(model);
            }
        }

        // ============================================================
        // EDITAR - GET
        // ============================================================

        [PermisoRequerido("CLIENTE.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var cliente = await _clienteService.GetClienteByIdAsync(id);

                if (cliente == null)
                {
                    TempData["MensajeError"] = "Cliente no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new ClienteEditDto
                {
                    IdCliente = cliente.IdCliente,
                    TipoCliente = cliente.TipoCliente,
                    TipoDocumento = cliente.TipoDocumento,
                    NumeroDocumento = cliente.NumeroDocumento,
                    NombreOrazonSocial = cliente.NombreOrazonSocial,
                    NombreComercial = cliente.NombreComercial,
                    Direccion = cliente.Direccion,
                    Distrito = cliente.Distrito,
                    Provincia = cliente.Provincia,
                    Departamento = cliente.Departamento,
                    Telefono = cliente.Telefono,
                    CorreoElectronico = cliente.CorreoElectronico,
                    ContactoNombre = cliente.ContactoNombre,
                    Observaciones = cliente.Observaciones,
                    Activo = cliente.Activo
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cliente para editar. IdCliente: {IdCliente}", id);
                TempData["MensajeError"] = "Error al cargar el cliente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // EDITAR - POST
        // ============================================================

        [PermisoRequerido("CLIENTE.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ClienteEditDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var result = await _clienteService.UpdateClienteAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["MensajeError"] = result.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cliente. IdCliente: {IdCliente}", model.IdCliente);
                TempData["MensajeError"] = "Error al actualizar el cliente.";
                return View(model);
            }
        }

        // ============================================================
        // DETALLES
        // ============================================================

        [PermisoRequerido("CLIENTE.VER")]
        [HttpGet]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var cliente = await _clienteService.GetClienteByIdAsync(id);

                if (cliente == null)
                {
                    TempData["MensajeError"] = "Cliente no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                return View(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del cliente. IdCliente: {IdCliente}", id);
                TempData["MensajeError"] = "Error al cargar los detalles del cliente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // ACTIVAR / DESACTIVAR
        // ============================================================

        [PermisoRequerido("CLIENTE.GESTIONAR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _clienteService.ToggleActivoAsync(id);

                if (result.Success)
                    TempData["MensajeExito"] = result.Message;
                else
                    TempData["MensajeError"] = result.Message;

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del cliente. IdCliente: {IdCliente}", id);
                TempData["MensajeError"] = "Error al cambiar el estado del cliente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // DESACTIVAR - VISTA DE CONFIRMACIÓN
        // ============================================================

        [PermisoRequerido("CLIENTE.GESTIONAR")]
        [HttpGet]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var cliente = await _clienteService.GetClienteByIdAsync(id);

                if (cliente == null)
                {
                    TempData["MensajeError"] = "Cliente no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                return View(cliente);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar cliente para desactivar. IdCliente: {IdCliente}", id);
                TempData["MensajeError"] = "Error al cargar el cliente.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // CLIENTES PARA SELECT - AJAX
        // ============================================================

        [PermisoRequerido("CLIENTE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetClientesSelect(string? term = null)
        {
            try
            {
                var clientes = await _clienteService.GetClientesActivosAsync();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    var termino = term.Trim();

                    clientes = clientes.Where(c =>
                        c.NombreOrazonSocial.Contains(
                            termino,
                            StringComparison.OrdinalIgnoreCase) ||
                        c.NumeroDocumento.Contains(
                            termino,
                            StringComparison.OrdinalIgnoreCase));
                }

                var result = clientes
                    .Take(20)
                    .Select(c => new
                    {
                        c.IdCliente,
                        c.NombreOrazonSocial,
                        c.NumeroDocumento,
                        display = $"{c.NombreOrazonSocial} - {c.NumeroDocumento}"
                    });

                return Json(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes para select");

                return Json(new
                {
                    success = false,
                    message = "Error al cargar clientes."
                });
            }
        }

        // ============================================================
        // VERIFICAR DOCUMENTO - AJAX
        // ============================================================

        [PermisoRequerido("CLIENTE.VER")]
        [HttpGet]
        public async Task<IActionResult> VerificarDocumento(
            string documento,
            int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                return Json(new
                {
                    exists = false,
                    error = false
                });
            }

            try
            {
                documento = documento.Trim();

                bool existe;

                if (excludeId.HasValue)
                {
                    existe = await _clienteService.DocumentoExistsAsync(
                        documento,
                        excludeId.Value);
                }
                else
                {
                    existe = await _clienteService.DocumentoExistsAsync(
                        documento);
                }

                return Json(new
                {
                    exists = existe
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al verificar documento de cliente: {Documento}",
                    documento);

                return Json(new
                {
                    exists = false,
                    error = true
                });
            }
        }

        // ============================================================
        // CLIENTE POR DOCUMENTO - AJAX
        // ============================================================

        [PermisoRequerido("CLIENTE.VER")]
        [HttpGet]
        public async Task<IActionResult> GetClienteByDocumento(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                return Json(new
                {
                    success = false,
                    message = "Documento requerido."
                });
            }

            try
            {
                documento = documento.Trim();

                var cliente = await _clienteService
                    .GetClienteByDocumentoAsync(documento);

                if (cliente == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cliente no encontrado."
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        cliente.IdCliente,
                        cliente.NombreOrazonSocial,
                        cliente.NumeroDocumento,
                        cliente.Telefono,
                        cliente.CorreoElectronico,
                        cliente.Direccion
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al buscar cliente por documento: {Documento}",
                    documento);

                return Json(new
                {
                    success = false,
                    message = "Error al buscar el cliente."
                });
            }
        }
    }
}
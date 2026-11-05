using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Inventario_Vidal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class ComprasController : Controller
    {
        private readonly ICompraService _compraService;
        private readonly IProveedorService _proveedorService;
        private readonly IMaterialService _materialService;
        private readonly ILogger<ComprasController> _logger;
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ISunatService _sunatService;
        private readonly IAuditoriaService _auditoriaService;

        private const int VigenciaVerificacionMinutos = 30;

        private static readonly ConcurrentDictionary<string, DateTime> _tokensRegistroCompra = new();
        private static readonly ConcurrentDictionary<string, byte> _comprasEnProceso = new();

        public ComprasController(
            ICompraService compraService,
            IProveedorService proveedorService,
            IMaterialService materialService,
            ILogger<ComprasController> logger,
            FabricacionesVidalEirlBdContext context,
            ISunatService sunatService,
            IAuditoriaService auditoriaService)
        {
            _compraService = compraService;
            _proveedorService = proveedorService;
            _materialService = materialService;
            _logger = logger;
            _context = context;
            _sunatService = sunatService;
            _auditoriaService = auditoriaService;
        }

        // ================================================================
        // INDEX
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> Index(
            CompraFiltroDto? filtro = null)
        {
            try
            {
                var compras =
                    await _compraService
                        .GetComprasAsync(
                            filtro
                        );

                var proveedores =
                    await _proveedorService
                        .GetProveedoresActivosAsync();

                ViewBag.Proveedores =
                    proveedores;

                ViewBag.Filtro =
                    filtro
                    ??
                    new CompraFiltroDto();

                return View(compras);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar compras"
                );

                TempData["MensajeError"] =
                    "Error al cargar las compras";

                return View(
                    new List<CompraListDto>()
                );
            }
        }

        // ================================================================
        // COMPRAR - ACCESO DIRECTO AL FORMULARIO DE REGISTRO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public IActionResult Comprar()
        {
            return RedirectToAction(nameof(Registrar));
        }

        // ================================================================
        // REGISTRAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.CREAR")]
        public async Task<IActionResult> Registrar()
        {
            try
            {
                var model = new CompraCreateDto
                {
                    Moneda = "PEN",
                    TipoComprobante = "Factura",
                    FechaEmisionComprobante = DateOnly.FromDateTime(DateTime.Today),
                    Detalles = new List<CompraDetalleCreateDto>(),
                    TokenSolicitud = Guid.NewGuid().ToString("N")
                };

                ChecklistRecepcionPreparadoDto? checklist = null;
                var checklistJson = HttpContext.Session.GetString("ConteoRecepcionCompra");

                if (!string.IsNullOrWhiteSpace(checklistJson))
                {
                    try
                    {
                        checklist = JsonSerializer.Deserialize<ChecklistRecepcionPreparadoDto>(checklistJson);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "No se pudo leer el checklist de recepción guardado en sesión.");
                        HttpContext.Session.Remove("ConteoRecepcionCompra");
                    }
                }

                if (checklist != null && checklist.IdProveedor > 0)
                {
                    model.IdProveedor = checklist.IdProveedor;
                    model.Detalles = checklist.Items.Select(item => new CompraDetalleCreateDto
                    {
                        IdMaterial = item.IdMaterial,
                        IdPresentacion = item.IdPresentacion,
                        Cantidad = item.Cantidad,
                        CostoUnitario = 0m
                    }).ToList();
                }

                LimpiarVerificacionSesion();

                await CargarFormularioCompraAsync(model.IdProveedor);
                ViewBag.ConteoRecepcion = checklist?.Items ?? new List<ConteoRecepcionPreparadoDto>();
                ViewBag.ChecklistRecepcion = checklist;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de registro de compra.");
                TempData["MensajeError"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ================================================================
        // REGISTRAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("COMPRA.CREAR")]
        [PermisoRequerido("COMPRA.RECIBIR")]
        public async Task<IActionResult> Registrar(CompraCreateDto model)
        {
            NormalizarComprobante(model);

            VerificacionesSunat? verificacionSunat = null;

            var claveVerificada = HttpContext.Session.GetString("CompraComprobanteVerificado");
            var claveEsperada = CrearClaveComprobanteSunat(new SunatVerificarComprobanteDto
            {
                IdProveedor = model.IdProveedor,
                TipoComprobante = model.TipoComprobante,
                Serie = model.SerieComprobante,
                Numero = model.CorrelativoComprobante,
                FechaEmision = model.FechaEmisionComprobante,
                Monto = model.MontoTotalComprobante
            });

            if (string.IsNullOrWhiteSpace(claveVerificada) ||
                !string.Equals(claveEsperada, claveVerificada, StringComparison.Ordinal))
            {
                ModelState.AddModelError(string.Empty,
                    "Debe verificar en SUNAT este mismo comprobante antes de registrar la compra.");
            }
            else
            {
                verificacionSunat = await ObtenerVerificacionVigenteAsync(claveEsperada);

                if (verificacionSunat == null)
                {
                    ModelState.AddModelError(string.Empty,
                        "La verificación en SUNAT de este comprobante venció o no es válida. Verifícalo nuevamente antes de registrar la compra.");
                }
            }

            if (model.Detalles == null || model.Detalles.Count == 0)
            {
                ModelState.AddModelError(nameof(model.Detalles),
                    "La compra debe contener al menos un material.");
            }
            else if (model.Detalles.Any(d =>
                d.IdMaterial <= 0 ||
                !d.IdPresentacion.HasValue ||
                d.IdPresentacion.Value <= 0 ||
                d.Cantidad <= 0 ||
                d.CostoUnitario <= 0))
            {
                ModelState.AddModelError(nameof(model.Detalles),
                    "Revisa los materiales, presentaciones, cantidades y precios.");
            }

            if (model.Detalles != null && model.Detalles.Count > 0 &&
                model.Igv >= 0 && model.Igv <= 100 &&
                model.MontoTotalComprobante > 0)
            {
                var subtotal = model.Detalles.Sum(d => d.Cantidad * d.CostoUnitario);
                var importeIgv = Math.Round(subtotal * (model.Igv / 100m), 2);
                var totalCalculado = Math.Round(subtotal + importeIgv, 2);

                if (Math.Abs(totalCalculado - model.MontoTotalComprobante) > 0.01m)
                {
                    ModelState.AddModelError(nameof(model.MontoTotalComprobante),
                        $"El total calculado de la compra ({model.Moneda} {totalCalculado:N2}) no coincide con el monto del comprobante ({model.Moneda} {model.MontoTotalComprobante:N2}).");
                }
            }

            if (model.Detalles != null && model.Detalles.Count > 0)
            {
                var conDiferencia = model.Detalles.Count(d =>
                    d.CantidadContada.HasValue &&
                    Math.Abs(d.CantidadContada.Value - d.Cantidad) > 0.0001m);

                if (conDiferencia > 0)
                {
                    ModelState.AddModelError(nameof(model.Detalles),
                        $"Hay {conDiferencia} material(es) con diferencia entre la cantidad de la factura y el conteo del escáner. Corrige las cantidades antes de registrar.");
                }

                if (!model.ConfirmaConteoFisico)
                {
                    ModelState.AddModelError(string.Empty,
                        "El jefe de almacén o usuario autorizado debe confirmar el conteo físico antes de registrar la compra.");
                }
            }

            if (string.IsNullOrWhiteSpace(model.TokenSolicitud))
            {
                ModelState.AddModelError(string.Empty,
                    "La solicitud no es válida. Recarga la página e inténtalo nuevamente.");
            }

            if (!ModelState.IsValid)
            {
                return await VolverAMostrarRegistrarAsync(model);
            }

            var token = model.TokenSolicitud!.Trim();
            var claveProceso = $"{model.IdProveedor}|{model.NumeroComprobante}".ToUpperInvariant();

            if (!_comprasEnProceso.TryAdd(claveProceso, 0))
            {
                ModelState.AddModelError(string.Empty,
                    "Este comprobante ya se está registrando. Espera unos segundos y revisa el listado de compras.");

                return await VolverAMostrarRegistrarAsync(model);
            }

            var tokenRegistrado = false;

            try
            {
                var limite = DateTime.UtcNow.AddHours(-12);
                foreach (var item in _tokensRegistroCompra)
                {
                    if (item.Value < limite)
                    {
                        _tokensRegistroCompra.TryRemove(item.Key, out _);
                    }
                }

                tokenRegistrado = _tokensRegistroCompra.TryAdd(token, DateTime.UtcNow);

                if (!tokenRegistrado)
                {
                    ModelState.AddModelError(string.Empty,
                        "Esta solicitud ya fue enviada. Revisa el listado de compras antes de volver a intentar.");

                    return await VolverAMostrarRegistrarAsync(model);
                }

                var existe = await _context.Compras
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.IdProveedor == model.IdProveedor &&
                        c.NumeroComprobante == model.NumeroComprobante);

                if (existe)
                {
                    ModelState.AddModelError(nameof(model.NumeroComprobante),
                        "Este comprobante ya fue registrado para el proveedor seleccionado.");

                    HttpContext.Session.Remove("CompraComprobanteVerificado");
                    _tokensRegistroCompra.TryRemove(token, out _);

                    return await VolverAMostrarRegistrarAsync(model);
                }

                var usuarioId = ObtenerUsuarioId();
                if (usuarioId <= 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "No se pudo identificar al usuario de la sesión.");

                    _tokensRegistroCompra.TryRemove(token, out _);

                    return await VolverAMostrarRegistrarAsync(model);
                }

                var result = await _compraService.RegistrarYRecibirCompraAsync(model, usuarioId);

                if (result.Success)
                {
                    if (verificacionSunat != null && result.IdCompra.HasValue)
                    {
                        try
                        {
                            await VincularVerificacionAsync(verificacionSunat.IdVerificacion, result.IdCompra.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "La compra {IdCompra} se registró, pero no se pudo vincular la verificación SUNAT.",
                                result.IdCompra);
                        }
                    }

                    LimpiarVerificacionSesion();
                    HttpContext.Session.Remove("ConteoRecepcionCompra");

                    TempData["MensajeExito"] = result.Message;
                    TempData["SunatEstadoComprobante"] = verificacionSunat?.EstadoComprobante ?? string.Empty;
                    TempData["FechaEmisionComprobante"] =
                        model.FechaEmisionComprobante?.ToString("dd/MM/yyyy") ?? string.Empty;
                    TempData["MontoTotalComprobante"] =
                        model.MontoTotalComprobante.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                    return RedirectToAction(nameof(Detalles), new { id = result.IdCompra });
                }

                _tokensRegistroCompra.TryRemove(token, out _);
                TempData["MensajeError"] = result.Message;

                return await VolverAMostrarRegistrarAsync(model);
            }
            catch (Exception ex)
            {
                if (tokenRegistrado)
                {
                    _tokensRegistroCompra.TryRemove(token, out _);
                }

                _logger.LogError(ex, "Error al registrar compra");
                TempData["MensajeError"] = "Error al registrar la compra";

                return await VolverAMostrarRegistrarAsync(model);
            }
            finally
            {
                _comprasEnProceso.TryRemove(claveProceso, out _);
            }
        }

        private async Task<IActionResult> VolverAMostrarRegistrarAsync(CompraCreateDto model)
        {
            ModelState.Remove(nameof(model.TokenSolicitud));
            model.TokenSolicitud = Guid.NewGuid().ToString("N");
            model.ConfirmaConteoFisico = false;

            await CargarFormularioCompraAsync(model.IdProveedor);
            await CargarConteoRecepcionViewBagAsync();

            return View("Registrar", model);
        }

        // ================================================================
        // DETALLES
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> Detalles(
            int id)
        {
            try
            {
                var compra =
                    await _compraService
                        .GetCompraByIdAsync(id);

                if (compra == null)
                {
                    TempData["MensajeError"] =
                        "Compra no encontrada";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                return View(compra);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar detalles de compra"
                );

                TempData["MensajeError"] =
                    "Error al cargar los detalles";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // HISTORIAL DE PRECIOS POR MATERIAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> HistorialPrecios(int idMaterial)
        {
            if (idMaterial <= 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un material válido.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var materialExiste = await _context.Materiales
                    .AsNoTracking()
                    .AnyAsync(m => m.IdMaterial == idMaterial);

                if (!materialExiste)
                {
                    TempData["MensajeError"] = "El material indicado no existe.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.IdMaterial = idMaterial;

                var historial = await _compraService
                    .GetHistorialPreciosAsync(idMaterial);

                return View(historial);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar historial de precios del material ID {IdMaterial}",
                    idMaterial);

                TempData["MensajeError"] = "No se pudo cargar el historial de precios.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ================================================================
        // EVOLUCIÓN DE PRECIOS POR MATERIAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> EvolucionPrecios(int idMaterial)
        {
            if (idMaterial <= 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un material válido.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var materialExiste = await _context.Materiales
                    .AsNoTracking()
                    .AnyAsync(m => m.IdMaterial == idMaterial);

                if (!materialExiste)
                {
                    TempData["MensajeError"] = "El material indicado no existe.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.IdMaterial = idMaterial;

                var evolucion = await _compraService
                    .GetEvolucionPreciosAsync(idMaterial);

                return View(evolucion);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar evolución de precios del material ID {IdMaterial}",
                    idMaterial);

                TempData["MensajeError"] = "No se pudo cargar la evolución de precios.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ================================================================
        // COMPARAR PROVEEDORES POR MATERIAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> CompararProveedores(
            int idMaterial,
            decimal cantidad = 1m)
        {
            if (idMaterial <= 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un material válido.";
                return RedirectToAction(nameof(Index));
            }

            cantidad = cantidad <= 0 ? 1m : cantidad;

            try
            {
                var materialExiste = await _context.Materiales
                    .AsNoTracking()
                    .AnyAsync(m => m.IdMaterial == idMaterial && m.Activo);

                if (!materialExiste)
                {
                    TempData["MensajeError"] = "El material indicado no existe o está inactivo.";
                    return RedirectToAction(nameof(Index));
                }

                var model = await _compraService
                    .GetComparacionProveedoresActualAsync(idMaterial, cantidad);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al comparar proveedores del material {IdMaterial}",
                    idMaterial);

                TempData["MensajeError"] = "No se pudo cargar la comparación de proveedores.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ================================================================
        // AJAX - COMPARACIÓN DE PROVEEDORES DESDE REGISTRAR COMPRA
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> GetComparacionProveedoresActual(
            int idMaterial,
            decimal cantidadBase = 1m,
            int? idProveedorActual = null)
        {
            if (idMaterial <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Debe seleccionar un material válido."
                });
            }

            cantidadBase = cantidadBase <= 0 ? 1m : cantidadBase;

            try
            {
                var comparacion = await _compraService
                    .GetComparacionProveedoresActualAsync(idMaterial, cantidadBase);

                if (comparacion.IdMaterial <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No se encontró el material indicado."
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        comparacion.IdMaterial,
                        comparacion.CodigoBarras,
                        comparacion.Material,
                        comparacion.UnidadBase,
                        comparacion.CantidadRequerida,
                        comparacion.TotalProveedores,
                        comparacion.ProveedoresConPrecio,
                        comparacion.MonedasMixtas,
                        proveedores = comparacion.Proveedores.Select(p => new
                        {
                            p.IdProveedor,
                            p.Proveedor,
                            p.RucProveedor,
                            p.EsProveedorPrincipal,
                            p.RelacionProveedor,
                            esProveedorActual =
                                idProveedorActual.HasValue &&
                                p.IdProveedor == idProveedorActual.Value,
                            p.Presentacion,
                            p.FactorConversion,
                            p.CantidadPresentacionesNecesarias,
                            p.PrecioPresentacion,
                            p.PrecioUnidadBase,
                            p.CostoEstimado,
                            p.Moneda,
                            p.TipoPrecio,
                            p.VigenciaTexto,
                            p.UltimoPrecioCompra,
                            p.FechaUltimoPrecio,
                            p.UsaPrecioVigente,
                            p.EsMejorOpcion,
                            p.AhorroEstimadoVsPrincipal
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar comparación de proveedores para material {IdMaterial}",
                    idMaterial);

                return Json(new
                {
                    success = false,
                    message = "No se pudo cargar la comparación de proveedores."
                });
            }
        }

        // ================================================================
        // RECIBIR COMPRA
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("COMPRA.RECIBIR")]
        public async Task<IActionResult> Recibir(int id)
        {
            try
            {
                var usuarioId = ObtenerUsuarioId();
                if (usuarioId <= 0)
                {
                    TempData["MensajeError"] = "No se pudo identificar al usuario de la sesión.";
                    return RedirectToAction(nameof(Detalles), new { id });
                }

                var compra = await _compraService.GetCompraByIdAsync(id);
                if (compra == null)
                {
                    TempData["MensajeError"] = "Compra no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                if (!string.Equals(compra.Estado, "Registrada", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["MensajeError"] = "Solo una compra registrada puede ser recibida.";
                    return RedirectToAction(nameof(Detalles), new { id });
                }

                var result = await _compraService.RecibirCompraAsync(id, usuarioId);
                TempData[result.Success ? "MensajeExito" : "MensajeError"] = result.Message;

                return RedirectToAction(nameof(Detalles), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recibir compra {IdCompra}", id);
                TempData["MensajeError"] = "Error al recibir la compra.";
                return RedirectToAction(nameof(Detalles), new { id });
            }
        }

        // ================================================================
        // ANULAR
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("COMPRA.RECIBIR")]
        public async Task<IActionResult> Anular(
            int id)
        {
            try
            {
                var usuarioId =
                    ObtenerUsuarioId();

                if (usuarioId <= 0)
                {
                    TempData["MensajeError"] =
                        "No se pudo identificar al usuario de la sesión";

                    return RedirectToAction(
                        nameof(Detalles),
                        new
                        {
                            id
                        }
                    );
                }

                var compra =
                    await _compraService
                        .GetCompraByIdAsync(id);

                if (compra == null)
                {
                    TempData["MensajeError"] =
                        "Compra no encontrada";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                /*
                 * Regla del sistema:
                 *
                 * Solo una compra Registrada
                 * puede anularse sin afectar stock.
                 */

                if (
                    !string.Equals(
                        compra.Estado,
                        "Registrada",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    TempData["MensajeError"] =
                        "Solo una compra registrada puede ser anulada.";

                    return RedirectToAction(
                        nameof(Detalles),
                        new
                        {
                            id
                        }
                    );
                }

                var result =
                    await _compraService
                        .AnularCompraAsync(
                            id,
                            usuarioId
                        );

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
                    nameof(Detalles),
                    new
                    {
                        id
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al anular compra {Id}",
                    id
                );

                TempData["MensajeError"] =
                    "Error al anular la compra";

                return RedirectToAction(
                    nameof(Detalles),
                    new
                    {
                        id
                    }
                );
            }
        }

        // ================================================================
        // ================================================================
        // TARIFA SIN IGV PARA LA PRESENTACION SELECCIONADA
        // El precio es orientativo y siempre editable en la compra.
        // ================================================================
        [HttpGet]
        [PermisoRequerido("COMPRA.CREAR")]
        public async Task<IActionResult> GetTarifaProveedor(
            int idProveedor,
            int idMaterial,
            int idPresentacion,
            decimal cantidad,
            string moneda,
            DateOnly? fecha = null)
        {
            if (idProveedor <= 0 || idMaterial <= 0 || idPresentacion <= 0 ||
                cantidad <= 0 || (moneda != "PEN" && moneda != "USD"))
            {
                return BadRequest(new { success = false, message = "Datos de tarifa no válidos." });
            }

            try
            {
                var fechaConsulta = fecha ?? DateOnly.FromDateTime(DateTime.Today);

                var presentacion = await _context.PresentacionesMaterial
                    .AsNoTracking()
                    .Where(p => p.IdPresentacion == idPresentacion &&
                                p.IdMaterial == idMaterial && p.Activo)
                    .Select(p => new { p.FactorAUnidadBase })
                    .FirstOrDefaultAsync();

                if (presentacion == null || presentacion.FactorAUnidadBase <= 0)
                    return Json(new
                    {
                        success = true,
                        encontrada = false,
                        message = "No existe una presentación activa válida."
                    });

                // Los rangos tarifarios se evalúan sobre la cantidad en unidad base.
                var cantidadBase = cantidad * presentacion.FactorAUnidadBase;

                var tarifa = await _context.PreciosProveedores
                    .AsNoTracking()
                    .Where(p => p.IdMaterialProveedorNavigation.IdProveedor == idProveedor &&
                                p.IdMaterialProveedorNavigation.IdMaterial == idMaterial &&
                                p.EsVigente && p.Moneda == moneda &&
                                p.FechaInicio <= fechaConsulta &&
                                (p.FechaFin == null || p.FechaFin >= fechaConsulta) &&
                                p.CantidadMinima <= cantidadBase &&
                                (p.CantidadMaxima == null || p.CantidadMaxima >= cantidadBase) &&
                                (p.IdPresentacion == idPresentacion ||
                                    (p.IdPresentacion == null && presentacion.FactorAUnidadBase == 1m)))
                    .OrderByDescending(p => p.IdPresentacion == idPresentacion)
                    .ThenByDescending(p => p.CantidadMinima)
                    .ThenByDescending(p => p.FechaInicio)
                    .ThenByDescending(p => p.IdPrecioProveedor)
                    .Select(p => new
                    {
                        p.PrecioPresentacion,
                        p.Moneda,
                        p.TipoPrecio,
                        p.FechaInicio,
                        p.FechaFin
                    })
                    .FirstOrDefaultAsync();

                if (tarifa == null)
                    return Json(new
                    {
                        success = true,
                        encontrada = false,
                        message = "Sin tarifa vigente para esta presentación, cantidad, fecha y moneda. Ingresa el valor sin IGV de la factura."
                    });

                return Json(new
                {
                    success = true,
                    encontrada = true,
                    precioSinIgv = tarifa.PrecioPresentacion,
                    moneda = tarifa.Moneda,
                    tipo = tarifa.TipoPrecio,
                    fechaInicio = tarifa.FechaInicio.ToString("yyyy-MM-dd"),
                    fechaFin = tarifa.FechaFin?.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tarifa de proveedor {IdProveedor} y material {IdMaterial}", idProveedor, idMaterial);
                return StatusCode(500, new
                {
                    success = false,
                    message = "No se pudo consultar la tarifa. Puedes ingresar el valor sin IGV manualmente."
                });
            }
        }

        // MATERIALES POR PROVEEDOR
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> GetMaterialesByProveedor(int idProveedor)
        {
            try
            {
                var materiales = await _context.MaterialProveedores
                    .Where(mp => mp.IdProveedor == idProveedor && mp.IdMaterialNavigation.Activo)
                    .Select(mp => new
                    {
                        mp.IdMaterial,
                        mp.IdMaterialNavigation.Nombre,
                        mp.IdMaterialNavigation.CodigoBarras,
                        StockActual = mp.IdMaterialNavigation.StockActual,
                        StockMinimo = mp.IdMaterialNavigation.StockMinimo,
                        CostoUnitario = mp.CostoUnitario,
                        Moneda = mp.Moneda ?? "PEN",
                        EsPrincipal = mp.EsProveedorPrincipal,
                        UnidadCompra = mp.IdMaterialNavigation.PresentacionesMateriales
                            .Where(p => p.Activo)
                            .OrderByDescending(p => p.EsPredeterminada)
                            .ThenBy(p => p.IdPresentacion)
                            .Select(p => p.IdUnidadPresentacionNavigation.Nombre)
                            .FirstOrDefault() ?? mp.IdMaterialNavigation.IdUnidadConsumoNavigation.Nombre,
                        Categoria = mp.IdMaterialNavigation.IdCategoriaNavigation.Nombre
                    })
                    .OrderByDescending(m => m.EsPrincipal)
                    .ThenBy(m => m.Nombre)
                    .ToListAsync();

                return Json(new { success = true, data = materiales });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materiales por proveedor");
                return Json(new { success = false, message = "Error al cargar materiales" });
            }
        }

        // ================================================================
        // PROVEEDORES PARA SELECT
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> GetProveedoresSelect(
            string? term = null)
        {
            try
            {
                var proveedores =
                    await _proveedorService
                        .GetProveedoresActivosAsync();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    term =
                        term.ToLower();

                    proveedores =
                        proveedores.Where(
                            p =>
                                p.RazonSocial
                                    .ToLower()
                                    .Contains(term)
                                ||
                                p.Ruc
                                    .ToLower()
                                    .Contains(term)
                        );
                }

                var result =
                    proveedores
                        .Select(
                            p =>
                                new
                                {
                                    p.IdProveedor,
                                    p.RazonSocial,
                                    p.Ruc,

                                    display =
                                        $"{p.RazonSocial} - {p.Ruc}"
                                }
                        )
                        .Take(20);

                return Json(
                    new
                    {
                        success =
                            true,

                        data =
                            result
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener proveedores para select"
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        message =
                            "Error al cargar proveedores"
                    }
                );
            }
        }

        // ================================================================
        // MATERIALES PARA SELECT
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> GetMaterialesSelect(
            string? term = null)
        {
            try
            {
                var materiales =
                    await _materialService
                        .GetMaterialesActivosAsync();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    term =
                        term.ToLower();

                    materiales =
                        materiales.Where(
                            m =>
                                m.Nombre
                                    .ToLower()
                                    .Contains(term)
                                ||
                                (
                                    m.CodigoBarras != null
                                    &&
                                    m.CodigoBarras
                                        .ToLower()
                                        .Contains(term)
                                )
                        );
                }

                var result =
                    materiales
                        .Select(
                            m =>
                                new
                                {
                                    m.IdMaterial,
                                    m.Nombre,
                                    m.CodigoBarras,
                                    m.StockActual,

                                    display =
                                        $"{m.Nombre} ({m.CodigoBarras})"
                                }
                        )
                        .Take(20);

                return Json(
                    new
                    {
                        success =
                            true,

                        data =
                            result
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener materiales para select"
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        message =
                            "Error al cargar materiales"
                    }
                );
            }
        }

        // ================================================================
        // RESUMEN DE COMPRAS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> GetResumen(
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            try
            {
                var resumen =
                    await _compraService
                        .GetResumenComprasAsync(
                            desde,
                            hasta
                        );

                return Json(
                    new
                    {
                        success =
                            true,

                        data =
                            new
                            {
                                resumen.TotalCompras,
                                resumen.TotalGastado,
                                resumen.ComprasRegistradas,
                                resumen.ComprasAnuladas,
                                resumen.GastosPorProveedor,
                                resumen.GastosPorMes
                            }
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener resumen de compras"
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        message =
                            "Error al obtener resumen"
                    }
                );
            }
        }

        // ================================================================
        // MATERIALES MÁS COMPRADOS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.VER")]
        public async Task<IActionResult> GetMaterialesMasComprados(
            int cantidad = 10)
        {
            try
            {
                var materiales =
                    await _compraService
                        .GetMaterialesMasCompradosAsync(
                            cantidad
                        );

                return Json(
                    new
                    {
                        success =
                            true,

                        data =
                            materiales.Select(
                                m =>
                                    new
                                    {
                                        m.IdMaterial,
                                        m.Nombre,
                                        m.CantidadTotal,
                                        m.TotalGastado
                                    }
                            )
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener materiales más comprados"
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        message =
                            "Error al cargar datos"
                    }
                );
            }
        }

        // ================================================================
        // VERIFICAR COMPROBANTE
        // ================================================================

        [HttpGet]
        [PermisoRequerido("COMPRA.CREAR")]
        public async Task<IActionResult> VerificarComprobante(
            string numero,
            int idProveedor,
            string tipoComprobante)
        {
            try
            {
                numero =
                    numero?.Trim()
                    ??
                    string.Empty;

                tipoComprobante =
                    tipoComprobante?.Trim()
                    ??
                    string.Empty;

                HttpContext.Session.Remove(
                    "CompraComprobanteVerificado"
                );

                if (idProveedor <= 0)
                {
                    return Json(
                        new
                        {
                            success =
                                false,

                            verificado =
                                false,

                            message =
                                "Debe seleccionar un proveedor"
                        }
                    );
                }

                if (
                    string.IsNullOrWhiteSpace(
                        numero
                    )
                )
                {
                    return Json(
                        new
                        {
                            success =
                                false,

                            verificado =
                                false,

                            message =
                                "Debe ingresar el número de comprobante"
                        }
                    );
                }

                if (
                    string.IsNullOrWhiteSpace(
                        tipoComprobante
                    )
                )
                {
                    return Json(
                        new
                        {
                            success =
                                false,

                            verificado =
                                false,

                            message =
                                "Debe seleccionar el tipo de comprobante"
                        }
                    );
                }

                var proveedor =
                    await _context
                        .Proveedores
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            p =>
                                p.IdProveedor
                                ==
                                idProveedor
                                &&
                                p.Activo
                        );

                if (proveedor == null)
                {
                    return Json(
                        new
                        {
                            success =
                                false,

                            verificado =
                                false,

                            message =
                                "El proveedor no existe o está inactivo"
                        }
                    );
                }

                /*
                 * Verificación interna:
                 *
                 * proveedor + comprobante
                 * no deben repetirse.
                 */

                var existe =
                    await _context
                        .Compras
                        .AsNoTracking()
                        .AnyAsync(
                            c =>
                                c.IdProveedor
                                ==
                                idProveedor
                                &&
                                c.NumeroComprobante
                                ==
                                numero
                        );

                if (existe)
                {
                    return Json(
                        new
                        {
                            success =
                                true,

                            verificado =
                                false,

                            exists =
                                true,

                            message =
                                "Este comprobante ya fue registrado para el proveedor seleccionado"
                        }
                    );
                }

                var clave =
                    CrearClaveComprobante(
                        idProveedor,
                        tipoComprobante,
                        numero
                    );

                HttpContext.Session.SetString(
                    "CompraComprobanteVerificado",
                    clave
                );

                return Json(
                    new
                    {
                        success =
                            true,

                        verificado =
                            true,

                        exists =
                            false,

                        proveedor =
                            proveedor.RazonSocial,

                        ruc =
                            proveedor.Ruc,

                        message =
                            "Comprobante validado internamente para continuar con el registro",

                        sunatIntegrado =
                            false
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al verificar comprobante"
                );

                HttpContext.Session.Remove(
                    "CompraComprobanteVerificado"
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        verificado =
                            false,

                        error =
                            true,

                        message =
                            "No se pudo verificar el comprobante"
                    }
                );
            }
        }

        // ================================================================
        // VERIFICAR COMPROBANTE EN SUNAT
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("COMPRA.CREAR")]
        public async Task<IActionResult> VerificarComprobanteSunat(
            [FromBody] SunatVerificarComprobanteDto request,
            CancellationToken cancellationToken)
        {
            LimpiarVerificacionSesion();

            if (!ModelState.IsValid)
            {
                var error = ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .FirstOrDefault();

                return Json(new
                {
                    success = false,
                    verificado = false,
                    resultado = nameof(SunatResultadoVerificacion.DatosInvalidos),
                    message = error ?? "Los datos del comprobante no son válidos."
                });
            }

            var usuarioId = ObtenerUsuarioId();
            if (usuarioId <= 0)
            {
                return Json(new
                {
                    success = false,
                    verificado = false,
                    message = "No se pudo identificar al usuario de la sesión."
                });
            }

            var proveedor = await _context.Proveedores
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IdProveedor == request.IdProveedor && p.Activo, cancellationToken);

            if (proveedor == null)
            {
                return Json(new
                {
                    success = false,
                    verificado = false,
                    message = "El proveedor no existe o está inactivo."
                });
            }

            var numeroCompleto = $"{request.Serie.Trim().ToUpperInvariant()}-{request.Numero.Trim()}";
            var yaRegistrado = await _context.Compras
                .AsNoTracking()
                .AnyAsync(c =>
                    c.IdProveedor == request.IdProveedor &&
                    c.NumeroComprobante == numeroCompleto,
                    cancellationToken);

            if (yaRegistrado)
            {
                return Json(new
                {
                    success = true,
                    verificado = false,
                    exists = true,
                    message = "Este comprobante ya fue registrado para el proveedor seleccionado."
                });
            }

            var clave = CrearClaveComprobanteSunat(request);
            var estadoValidado = nameof(SunatResultadoVerificacion.Validado);
            var limiteReutilizacion = DateTime.Now.AddMinutes(-VigenciaVerificacionMinutos);

            var previa = await _context.VerificacionesSunat
                .AsNoTracking()
                .Where(v =>
                    v.ClaveComprobante == clave &&
                    v.IdUsuario == usuarioId &&
                    v.Resultado == estadoValidado &&
                    v.IdCompra == null &&
                    v.FechaConsulta >= limiteReutilizacion)
                .OrderByDescending(v => v.IdVerificacion)
                .FirstOrDefaultAsync(cancellationToken);

            if (previa != null)
            {
                HttpContext.Session.SetString("CompraComprobanteVerificado", clave);
                HttpContext.Session.SetString(
                    "CompraComprobanteVerificacionId",
                    previa.IdVerificacion.ToString(System.Globalization.CultureInfo.InvariantCulture));

                var advertencias = new List<string>();
                if (previa.EstadoRucCodigo != "00")
                    advertencias.Add($"El RUC del emisor figura como {previa.EstadoRuc}.");
                if (previa.CondicionDomicilioCodigo != "00")
                    advertencias.Add($"La condición de domicilio del emisor es {previa.CondicionDomicilio}.");

                return Json(new
                {
                    success = true,
                    verificado = true,
                    reutilizado = true,
                    configurado = true,
                    exists = false,
                    resultado = estadoValidado,
                    referencia = previa.ReferenciaConsulta,
                    proveedor = proveedor.RazonSocial,
                    ruc = proveedor.Ruc,
                    numeroComprobante = numeroCompleto,
                    estadoCp = previa.EstadoCp,
                    estadoComprobante = previa.EstadoComprobante,
                    estadoRuc = previa.EstadoRuc,
                    condicionDomicilio = previa.CondicionDomicilio,
                    domicilioHabido = previa.CondicionDomicilioCodigo == "00",
                    observaciones = (previa.ObservacionesSunat ?? string.Empty)
                        .Split(" | ", StringSplitOptions.RemoveEmptyEntries),
                    advertencias,
                    message = "Comprobante verificado correctamente en SUNAT."
                });
            }

            var resultado = await _sunatService.VerificarComprobanteAsync(
                proveedor.Ruc,
                request.TipoComprobante,
                request.Serie,
                request.Numero,
                request.FechaEmision!.Value,
                request.Monto,
                cancellationToken);

            if (!resultado.Configurado)
            {
                return Json(new
                {
                    success = false,
                    verificado = false,
                    configurado = false,
                    resultado = resultado.Resultado.ToString(),
                    message = resultado.Message
                });
            }

            long? idVerificacion = null;

            if (resultado.Resultado is not (SunatResultadoVerificacion.DatosInvalidos or SunatResultadoVerificacion.NoConfigurado))
            {
                try
                {
                    idVerificacion = await RegistrarVerificacionAsync(
                        request, proveedor.Ruc, numeroCompleto, clave, usuarioId, resultado);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "No se pudo guardar la evidencia de la consulta SUNAT {Referencia}",
                        resultado.ReferenciaConsulta);

                    return Json(new
                    {
                        success = false,
                        verificado = false,
                        configurado = true,
                        resultado = nameof(SunatResultadoVerificacion.NoConcluyente),
                        pendiente = true,
                        referencia = resultado.ReferenciaConsulta,
                        message = "La consulta a SUNAT se realizó, pero no se pudo guardar la evidencia. La validación queda pendiente; intenta nuevamente."
                    });
                }

                try
                {
                    await _auditoriaService.RegistrarAsync(
                        usuarioId,
                        "Compras.VerificacionesSunat",
                        "INSERT",
                        idVerificacion?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        null,
                        JsonSerializer.Serialize(new
                        {
                            Comprobante = numeroCompleto,
                            Resultado = resultado.Resultado.ToString(),
                            resultado.EstadoCp,
                            resultado.ReferenciaConsulta
                        }),
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Request.Headers.UserAgent.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo registrar en auditoría la consulta SUNAT {Referencia}", resultado.ReferenciaConsulta);
                }
            }

            if (resultado.Resultado != SunatResultadoVerificacion.Validado || !resultado.Verificado)
            {
                return Json(new
                {
                    success = resultado.Concluyente,
                    verificado = false,
                    configurado = true,
                    resultado = resultado.Resultado.ToString(),
                    pendiente = !resultado.Concluyente,
                    referencia = resultado.ReferenciaConsulta,
                    message = resultado.Message,
                    estadoCp = resultado.EstadoCp,
                    estadoComprobante = resultado.EstadoComprobante,
                    estadoRuc = resultado.EstadoRuc,
                    condicionDomicilio = resultado.CondicionDomicilio,
                    observaciones = resultado.Observaciones,
                    errorCode = resultado.ErrorCode
                });
            }

            HttpContext.Session.SetString("CompraComprobanteVerificado", clave);
            HttpContext.Session.SetString(
                "CompraComprobanteVerificacionId",
                idVerificacion!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return Json(new
            {
                success = true,
                verificado = true,
                configurado = true,
                exists = false,
                resultado = resultado.Resultado.ToString(),
                referencia = resultado.ReferenciaConsulta,
                proveedor = proveedor.RazonSocial,
                ruc = proveedor.Ruc,
                numeroComprobante = numeroCompleto,
                estadoCp = resultado.EstadoCp,
                estadoComprobante = resultado.EstadoComprobante,
                estadoRuc = resultado.EstadoRuc,
                condicionDomicilio = resultado.CondicionDomicilio,
                domicilioHabido = resultado.DomicilioHabido,
                observaciones = resultado.Observaciones,
                advertencias = resultado.Advertencias,
                message = resultado.Message
            });
        }

        // ================================================================
        // MÉTODOS PRIVADOS
        // ================================================================

        private void LimpiarVerificacionSesion()
        {
            HttpContext.Session.Remove("CompraComprobanteVerificado");
            HttpContext.Session.Remove("CompraComprobanteVerificacionId");
        }

        private async Task<VerificacionesSunat?> ObtenerVerificacionVigenteAsync(string claveEsperada)
        {
            var idTexto = HttpContext.Session.GetString("CompraComprobanteVerificacionId");

            if (!long.TryParse(idTexto, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var idVerificacion) ||
                idVerificacion <= 0)
            {
                return null;
            }

            var usuarioId = ObtenerUsuarioId();
            var limite = DateTime.Now.AddMinutes(-VigenciaVerificacionMinutos);

            var verificacion = await _context.VerificacionesSunat
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVerificacion == idVerificacion);

            if (verificacion == null ||
                verificacion.Resultado != nameof(SunatResultadoVerificacion.Validado) ||
                verificacion.ClaveComprobante != claveEsperada ||
                verificacion.IdUsuario != usuarioId ||
                verificacion.IdCompra != null ||
                verificacion.FechaConsulta < limite)
            {
                return null;
            }

            return verificacion;
        }

        private async Task<long> RegistrarVerificacionAsync(
            SunatVerificarComprobanteDto request,
            string rucEmisor,
            string numeroCompleto,
            string clave,
            int usuarioId,
            SunatComprobanteResultadoDto resultado)
        {
            var entidad = new VerificacionesSunat
            {
                IdProveedor = request.IdProveedor,
                IdUsuario = usuarioId,
                TipoComprobante = Truncar(request.TipoComprobante.Trim(), 20)!,
                CodigoComprobante = Truncar(resultado.CodigoComprobante, 2) ?? string.Empty,
                RucEmisor = rucEmisor,
                NumeroComprobante = numeroCompleto,
                FechaEmision = request.FechaEmision!.Value,
                Monto = Math.Round(request.Monto, 2),
                ClaveComprobante = clave,
                Resultado = resultado.Resultado.ToString(),
                EstadoCp = resultado.EstadoCp.HasValue ? (byte?)resultado.EstadoCp.Value : null,
                EstadoComprobante = Truncar(resultado.EstadoComprobante, 30),
                EstadoRucCodigo = Truncar(resultado.EstadoRucCodigo, 2),
                EstadoRuc = Truncar(resultado.EstadoRuc, 40),
                CondicionDomicilioCodigo = Truncar(resultado.CondicionDomicilioCodigo, 2),
                CondicionDomicilio = Truncar(resultado.CondicionDomicilio, 30),
                CodigoErrorSunat = Truncar(resultado.ErrorCode, 50),
                MensajeSunat = Truncar(resultado.MensajeSunat, 500),
                ObservacionesSunat = Truncar(string.Join(" | ", resultado.Observaciones), 1000),
                HttpStatus = resultado.HttpStatus.HasValue ? (short?)resultado.HttpStatus.Value : null,
                ReferenciaConsulta = resultado.ReferenciaConsulta,
                FechaConsulta = DateTime.Now,
                Ip = Truncar(HttpContext.Connection.RemoteIpAddress?.ToString(), 50)
            };

            _context.VerificacionesSunat.Add(entidad);
            await _context.SaveChangesAsync();

            return entidad.IdVerificacion;
        }

        private async Task VincularVerificacionAsync(long idVerificacion, int idCompra)
        {
            try
            {
                await _context.VerificacionesSunat
                    .Where(v => v.IdVerificacion == idVerificacion && v.IdCompra == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdCompra, (int?)idCompra));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "No se pudo vincular la verificación SUNAT {IdVerificacion} con la compra {IdCompra}",
                    idVerificacion,
                    idCompra);
            }
        }

        private static string? Truncar(string? valor, int maximo)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return valor;
            }

            return valor.Length <= maximo ? valor : valor[..maximo];
        }

        private int ObtenerUsuarioId()
        {
            return HttpContext.Session
                .GetInt32("IdUsuario")
                ?? 0;
        }

        private async Task CargarFormularioCompraAsync(int idProveedor = 0)
        {
            ViewBag.Proveedores = await _proveedorService.GetProveedoresActivosAsync();

            if (idProveedor <= 0)
            {
                ViewBag.Materiales = new List<MaterialListDto>();
                return;
            }

            var idsMateriales = await _context.MaterialProveedores
                .AsNoTracking()
                .Where(mp => mp.IdProveedor == idProveedor && mp.IdMaterialNavigation.Activo)
                .Select(mp => mp.IdMaterial)
                .ToListAsync();

            var materiales = await _materialService.GetMaterialesActivosAsync();
            ViewBag.Materiales = materiales
                .Where(m => idsMateriales.Contains(m.IdMaterial))
                .OrderBy(m => m.Nombre)
                .ToList();
        }

        private async Task CargarConteoRecepcionViewBagAsync()
        {
            var checklistJson = HttpContext.Session.GetString("ConteoRecepcionCompra");

            if (string.IsNullOrWhiteSpace(checklistJson))
            {
                ViewBag.ConteoRecepcion = new List<ConteoRecepcionPreparadoDto>();
                ViewBag.ChecklistRecepcion = null;
                return;
            }

            try
            {
                var checklist = JsonSerializer.Deserialize<ChecklistRecepcionPreparadoDto>(checklistJson);

                ViewBag.ChecklistRecepcion = checklist;
                ViewBag.ConteoRecepcion = checklist?.Items ?? new List<ConteoRecepcionPreparadoDto>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "No se pudo leer el checklist de recepción guardado en sesión.");
                HttpContext.Session.Remove("ConteoRecepcionCompra");
                ViewBag.ConteoRecepcion = new List<ConteoRecepcionPreparadoDto>();
                ViewBag.ChecklistRecepcion = null;
            }

            await Task.CompletedTask;
        }

        private static string CrearClaveComprobante(
            int idProveedor,
            string? tipoComprobante,
            string? numero)
        {
            return string.Join(
                "|",
                idProveedor.ToString(),
                (
                    tipoComprobante
                    ??
                    string.Empty
                )
                .Trim()
                .ToUpperInvariant(),
                (
                    numero
                    ??
                    string.Empty
                )
                .Trim()
                .ToUpperInvariant()
            );
        }
        private static void NormalizarComprobante(CompraCreateDto model)
        {
            model.SerieComprobante = (model.SerieComprobante ?? string.Empty).Trim().ToUpperInvariant();
            model.CorrelativoComprobante = (model.CorrelativoComprobante ?? string.Empty).Trim();
            model.TipoComprobante = (model.TipoComprobante ?? string.Empty).Trim();
            model.Moneda = string.IsNullOrWhiteSpace(model.Moneda)
                ? "PEN"
                : model.Moneda.Trim().ToUpperInvariant();

            model.NumeroComprobante = !string.IsNullOrWhiteSpace(model.SerieComprobante) &&
                                      !string.IsNullOrWhiteSpace(model.CorrelativoComprobante)
                ? $"{model.SerieComprobante}-{model.CorrelativoComprobante}"
                : string.Empty;
        }

        private static string CrearClaveComprobanteSunat(SunatVerificarComprobanteDto request)
        {
            var serie = request.Serie.Trim().ToUpperInvariant();
            var numero = request.Numero.Trim().TrimStart('0');
            if (string.IsNullOrEmpty(numero)) numero = "0";

            return string.Join("|",
                request.IdProveedor,
                request.TipoComprobante.Trim().ToUpperInvariant(),
                serie,
                numero,
                request.FechaEmision?.ToString("yyyyMMdd") ?? string.Empty,
                request.Monto.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }

    }
}
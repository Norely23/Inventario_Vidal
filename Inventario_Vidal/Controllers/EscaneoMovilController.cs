using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    [PermitirAccesoPublico]
    public class EscaneoMovilController : Controller
    {
        private readonly IEscaneoMovilService _escaneoMovilService;
        private readonly IMaterialService _materialService;
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<EscaneoMovilController> _logger;

        public EscaneoMovilController(
            IEscaneoMovilService escaneoMovilService,
            IMaterialService materialService,
            FabricacionesVidalEirlBdContext context,
            ILogger<EscaneoMovilController> logger)
        {
            _escaneoMovilService = escaneoMovilService;
            _materialService = materialService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Escanear(string token)
        {
            var tokenValido =
                _escaneoMovilService.ExisteSesionValida(token);

            ViewBag.TokenValido = tokenValido;
            ViewBag.Token = token;
            ViewBag.Proceso = string.Empty;
            ViewBag.IdProveedor = null;
            ViewBag.ProveedorBloqueado = false;
            ViewBag.Proveedores = new List<object>();

            if (!tokenValido)
                return View();

            var proceso =
                _escaneoMovilService.ObtenerProceso(token);

            ViewBag.Proceso = proceso;

            if (proceso == TiposProcesoEscaneo.Recepcion)
            {
                var proveedores =
                    await _context.Proveedores
                        .AsNoTracking()
                        .Where(p => p.Activo)
                        .OrderBy(p => p.RazonSocial)
                        .Select(p => new
                        {
                            p.IdProveedor,
                            p.RazonSocial,
                            p.Ruc
                        })
                        .ToListAsync();

                ViewBag.Proveedores = proveedores;
                ViewBag.IdProveedor =
                    _escaneoMovilService.ObtenerIdProveedor(token);
                ViewBag.ProveedorBloqueado =
                    _escaneoMovilService.TieneEscaneos(token);
            }

            _escaneoMovilService
                .MarcarCelularConectado(token);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SeleccionarProveedor(
            string token,
            int idProveedor)
        {
            try
            {
                if (!_escaneoMovilService.ExisteSesionValida(token))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "La sesión de escaneo expiró."
                    });
                }

                var proceso =
                    _escaneoMovilService.ObtenerProceso(token);

                if (proceso != TiposProcesoEscaneo.Recepcion)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "El proveedor solo se selecciona en una recepción."
                    });
                }

                if (idProveedor <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Selecciona un proveedor válido."
                    });
                }

                var proveedor =
                    await _context.Proveedores
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p =>
                            p.IdProveedor == idProveedor &&
                            p.Activo);

                if (proveedor == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "El proveedor no existe o está inactivo."
                    });
                }

                var proveedorActual =
                    _escaneoMovilService
                        .ObtenerIdProveedor(token);

                if (_escaneoMovilService.TieneEscaneos(token) &&
                    proveedorActual.HasValue &&
                    proveedorActual.Value != idProveedor)
                {
                    return Json(new
                    {
                        success = false,
                        bloqueado = true,
                        idProveedor = proveedorActual,
                        message =
                            "El proveedor quedó bloqueado porque ya existen materiales en el checklist. Cierra esta sesión y genera un nuevo QR para cambiarlo."
                    });
                }

                if (!_escaneoMovilService
                    .AsignarProveedor(token, idProveedor))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "No se pudo asociar el proveedor a la sesión."
                    });
                }

                return Json(new
                {
                    success = true,
                    idProveedor = proveedor.IdProveedor,
                    proveedor = proveedor.RazonSocial,
                    ruc = proveedor.Ruc,
                    bloqueado =
                        _escaneoMovilService.TieneEscaneos(token),
                    message =
                        "Proveedor seleccionado. Ya puedes activar la cámara."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al seleccionar proveedor {IdProveedor} en sesión móvil",
                    idProveedor);

                return Json(new
                {
                    success = false,
                    message =
                        "No se pudo seleccionar el proveedor."
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BuscarMaterial(
            string token,
            string codigoBarras)
        {
            try
            {
                if (!_escaneoMovilService.ExisteSesionValida(token))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "La sesión de escaneo expiró."
                    });
                }

                if (string.IsNullOrWhiteSpace(codigoBarras))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "Ingresa o escanea un código de barras."
                    });
                }

                var proceso =
                    _escaneoMovilService.ObtenerProceso(token);

                if (string.IsNullOrWhiteSpace(proceso))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "No se pudo determinar el tipo de escaneo."
                    });
                }

                var idProveedor =
                    _escaneoMovilService.ObtenerIdProveedor(token);

                if (proceso == TiposProcesoEscaneo.Recepcion &&
                    (!idProveedor.HasValue ||
                     idProveedor.Value <= 0))
                {
                    return Json(new
                    {
                        success = false,
                        requiereProveedor = true,
                        message =
                            "Selecciona primero el proveedor de la recepción."
                    });
                }

                codigoBarras = codigoBarras.Trim();

                // Entrega: el QR de un paquete identifica también su material.
                long? idPaqueteIdentificado = null;
                int? idMaterialPaquete = null;
                if (proceso == TiposProcesoEscaneo.Entrega &&
                    (codigoBarras.StartsWith("VIDAL-PQ-", StringComparison.OrdinalIgnoreCase) ||
                     codigoBarras.StartsWith("PKG:", StringComparison.OrdinalIgnoreCase)))
                {
                    var numero = codigoBarras.StartsWith("PKG:", StringComparison.OrdinalIgnoreCase)
                        ? codigoBarras[4..] : codigoBarras[9..];
                    if (!long.TryParse(numero, out var idQr) || idQr <= 0)
                        return Json(new { success = false, message = "QR de paquete inválido." });
                    var paquete = await _context.PaquetesInventarios.AsNoTracking()
                        .Where(p => p.IdPaquete == idQr && p.CantidadActualUnidadBase > 0 &&
                            (p.Estado == "Abierto" || p.Estado == "Sellado"))
                        .Select(p => new { p.IdPaquete, p.IdMaterial })
                        .FirstOrDefaultAsync();
                    if (paquete == null)
                        return Json(new { success = false, message = "Paquete físico inexistente o sin disponibilidad." });
                    idPaqueteIdentificado = paquete.IdPaquete;
                    idMaterialPaquete = paquete.IdMaterial;
                }
                MaterialListDto? material;
                if (idMaterialPaquete.HasValue)
                {
                    var detalle = await _materialService.GetMaterialByIdAsync(idMaterialPaquete.Value);
                    material = detalle == null ? null : new MaterialListDto
                    {
                        IdMaterial = detalle.IdMaterial,
                        Nombre = detalle.Nombre,
                        CodigoBarras = detalle.CodigoBarras,
                        Categoria = detalle.Categoria,
                        UnidadConsumo = detalle.UnidadConsumo,
                        StockActual = detalle.StockActual,
                        Activo = detalle.Activo
                    };
                }
                else
                {
                    material = await _materialService.GetMaterialByCodigoBarrasAsync(codigoBarras);
                }

                if (material == null)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "No se encontró ningún material con ese código de barras."
                    });
                }

                if (!material.Activo)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            $"El material \"{material.Nombre}\" se encuentra inactivo."
                    });
                }

                bool? esProveedorPrincipal = null;
                decimal? costoReferencia = null;
                string monedaReferencia = "PEN";
                string proveedorNombre = string.Empty;

                if (proceso == TiposProcesoEscaneo.Recepcion)
                {
                    var relacion =
                        await _context.MaterialProveedores
                            .AsNoTracking()
                            .Where(mp =>
                                mp.IdProveedor == idProveedor!.Value &&
                                mp.IdMaterial == material.IdMaterial)
                            .Select(mp => new
                            {
                                mp.EsProveedorPrincipal,
                                mp.CostoUnitario,
                                Moneda =
                                    mp.Moneda ?? "PEN",
                                Proveedor =
                                    mp.IdProveedorNavigation.RazonSocial
                            })
                            .FirstOrDefaultAsync();

                    if (relacion == null)
                    {
                        return Json(new
                        {
                            success = false,
                            materialNoAsociado = true,
                            message =
                                $"El material \"{material.Nombre}\" no está asociado al proveedor seleccionado."
                        });
                    }

                    esProveedorPrincipal =
                        relacion.EsProveedorPrincipal;
                    costoReferencia =
                        relacion.CostoUnitario;
                    monedaReferencia =
                        relacion.Moneda;
                    proveedorNombre =
                        relacion.Proveedor;
                }

                var presentaciones =
                    new List<PresentacionEscaneoDto>();

                if (proceso == TiposProcesoEscaneo.Recepcion)
                {
                    var lista =
                        await _materialService
                            .GetPresentacionesByMaterialAsync(
                                material.IdMaterial);

                    presentaciones =
                        lista
                            .Where(p => p.Activo)
                            .OrderByDescending(
                                p => p.EsPredeterminada)
                            .ThenBy(p => p.Nombre)
                            .Select(p =>
                                new PresentacionEscaneoDto
                                {
                                    IdPresentacion =
                                        p.IdPresentacion,
                                    Nombre =
                                        p.Nombre,
                                    UnidadPresentacion =
                                        p.UnidadPresentacion,
                                    AbreviaturaUnidad =
                                        p.AbreviaturaUnidad,
                                    FactorAUnidadBase =
                                        p.FactorAUnidadBase,
                                    EsPredeterminada =
                                        p.EsPredeterminada
                                })
                            .ToList();

                    if (presentaciones.Count == 0)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"El material \"{material.Nombre}\" no tiene presentaciones activas configuradas."
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    proceso,
                    data = new
                    {
                        material.IdMaterial,
                        idPaquete = idPaqueteIdentificado,
                        material.Nombre,
                        material.CodigoBarras,
                        material.Categoria,
                        material.UnidadConsumo,
                        material.StockActual,
                        material.Activo,
                        esProveedorPrincipal,
                        costoReferencia,
                        monedaReferencia,
                        proveedorNombre,
                        presentaciones
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al buscar material por código {CodigoBarras}",
                    codigoBarras);

                return Json(new
                {
                    success = false,
                    message =
                        "No se pudo consultar el material."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AgregarMaterial(
            string token,
            int idMaterial,
            decimal cantidad,
            int? idPresentacion,
            long? idPaquete = null)
        {
            try
            {
                if (!_escaneoMovilService.ExisteSesionValida(token))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "La sesión de escaneo expiró."
                    });
                }

                if (idMaterial <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "El material seleccionado no es válido."
                    });
                }

                if (cantidad <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "La cantidad debe ser mayor que cero."
                    });
                }

                var proceso =
                    _escaneoMovilService.ObtenerProceso(token);

                if (string.IsNullOrWhiteSpace(proceso))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "No se pudo determinar el tipo de escaneo."
                    });
                }

                var material =
                    await _materialService
                        .GetMaterialByIdAsync(idMaterial);

                if (material == null ||
                    !material.Activo)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "El material no existe o está inactivo."
                    });
                }

                if (proceso == TiposProcesoEscaneo.Entrega && idPaquete.HasValue)
                {
                    var paqueteValido = await _context.PaquetesInventarios.AsNoTracking()
                        .AnyAsync(p => p.IdPaquete == idPaquete.Value &&
                            p.IdMaterial == idMaterial && p.CantidadActualUnidadBase > 0 &&
                            (p.Estado == "Abierto" || p.Estado == "Sellado"));
                    if (!paqueteValido)
                        return Json(new { success = false, message = "El paquete no corresponde al material o ya no está disponible." });
                }

                string? presentacionNombre = null;
                string? unidadPresentacion = null;
                decimal? factorAUnidadBase = null;

                if (proceso == TiposProcesoEscaneo.Recepcion)
                {
                    var idProveedor =
                        _escaneoMovilService
                            .ObtenerIdProveedor(token);

                    if (!idProveedor.HasValue ||
                        idProveedor.Value <= 0)
                    {
                        return Json(new
                        {
                            success = false,
                            requiereProveedor = true,
                            message =
                                "Selecciona primero el proveedor de la recepción."
                        });
                    }

                    var materialAsociado =
                        await _context.MaterialProveedores
                            .AsNoTracking()
                            .AnyAsync(mp =>
                                mp.IdProveedor == idProveedor.Value &&
                                mp.IdMaterial == idMaterial);

                    if (!materialAsociado)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"El material \"{material.Nombre}\" no está asociado al proveedor seleccionado."
                        });
                    }

                    if (!idPresentacion.HasValue ||
                        idPresentacion.Value <= 0)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                "Selecciona la presentación recibida."
                        });
                    }

                    var presentacion =
                        await _context.PresentacionesMaterial
                            .Include(
                                p => p.IdUnidadPresentacionNavigation)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p =>
                                p.IdPresentacion ==
                                    idPresentacion.Value &&
                                p.IdMaterial ==
                                    idMaterial &&
                                p.Activo);

                    if (presentacion == null)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                "La presentación seleccionada no es válida."
                        });
                    }

                    if (!presentacion
                            .IdUnidadPresentacionNavigation
                            .EsFraccionable &&
                        cantidad != decimal.Truncate(cantidad))
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"La presentación \"{presentacion.Nombre}\" solo admite cantidades enteras."
                        });
                    }

                    presentacionNombre =
                        presentacion.Nombre;

                    unidadPresentacion =
                        presentacion
                            .IdUnidadPresentacionNavigation
                            .Nombre;

                    factorAUnidadBase =
                        presentacion.FactorAUnidadBase;
                }

                if (proceso == TiposProcesoEscaneo.Entrega)
                {
                    if (material.StockActual <= 0)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"El material \"{material.Nombre}\" no tiene stock disponible."
                        });
                    }

                    var acumulado = _escaneoMovilService.ObtenerCantidadAcumulada(token, idMaterial);
                    if (cantidad + acumulado > material.StockActual)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                $"La cantidad acumulada excede el stock disponible. Disponible: {material.StockActual:0.##} {material.UnidadConsumo}; escaneado: {acumulado:0.##}."
                        });
                    }
                }

                var item =
                    new EscaneoMovilItemDto
                    {
                        IdMaterial =
                            material.IdMaterial,
                        IdPaquete = proceso == TiposProcesoEscaneo.Entrega ? idPaquete : null,
                        Nombre =
                            material.Nombre,
                        CodigoBarras =
                            material.CodigoBarras,
                        Categoria =
                            material.Categoria,
                        UnidadConsumo =
                            material.UnidadConsumo,
                        StockActual =
                            material.StockActual,
                        Activo =
                            material.Activo,
                        Cantidad =
                            cantidad,
                        IdPresentacion =
                            idPresentacion,
                        Presentacion =
                            presentacionNombre,
                        UnidadPresentacion =
                            unidadPresentacion,
                        FactorAUnidadBase =
                            factorAUnidadBase,
                        FechaEscaneo =
                            DateTime.Now
                    };

                _escaneoMovilService
                    .AgregarEscaneo(token, item);

                return Json(new
                {
                    success = true,
                    proveedorBloqueado =
                        proceso ==
                            TiposProcesoEscaneo.Recepcion,
                    message =
                        $"{material.Nombre} agregado al checklist.",
                    data = item
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al agregar material {IdMaterial} a sesión móvil",
                    idMaterial);

                return Json(new
                {
                    success = false,
                    message =
                        "No se pudo agregar el material."
                });
            }
        }

        [HttpGet]
        public IActionResult Estado(string token)
        {
            if (!_escaneoMovilService.ExisteSesionValida(token))
            {
                return Json(new
                {
                    success = false,
                    activa = false,
                    message =
                        "La sesión de escaneo finalizó o expiró."
                });
            }

            return Json(new
            {
                success = true,
                activa = true,
                proceso =
                    _escaneoMovilService.ObtenerProceso(token),
                idProveedor =
                    _escaneoMovilService.ObtenerIdProveedor(token),
                tieneEscaneos =
                    _escaneoMovilService.TieneEscaneos(token)
            });
        }
    }
}

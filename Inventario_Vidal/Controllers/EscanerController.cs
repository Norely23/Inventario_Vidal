using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    [PermisoRequerido("ESCANER.USAR")]
    public class EscanerController : Controller
    {
        private readonly IEscaneoMovilService _escaneoMovilService;
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<EscanerController> _logger;

        public EscanerController(
            IEscaneoMovilService escaneoMovilService,
            FabricacionesVidalEirlBdContext context,
            ILogger<EscanerController> logger)
        {
            _escaneoMovilService = escaneoMovilService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> IniciarSesion(string proceso, int? idProveedor = null)
        {
            try
            {
                if (proceso != TiposProcesoEscaneo.Recepcion &&
                    proceso != TiposProcesoEscaneo.Entrega)
                {
                    return Json(new
                    {
                        success = false,
                        message = "El tipo de escaneo no es válido."
                    });
                }

                var idUsuario =
                    HttpContext.Session.GetInt32("IdUsuario");

                if (!idUsuario.HasValue ||
                    idUsuario.Value <= 0)
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "No se pudo identificar al usuario de la sesión."
                    });
                }

                int? idProveedorInicial = null;

                if (proceso == TiposProcesoEscaneo.Recepcion &&
                    idProveedor.HasValue &&
                    idProveedor.Value > 0)
                {
                    var proveedorExiste =
                        await _context.Proveedores
                            .AsNoTracking()
                            .AnyAsync(p =>
                                p.IdProveedor == idProveedor.Value &&
                                p.Activo);

                    if (!proveedorExiste)
                    {
                        return Json(new
                        {
                            success = false,
                            message =
                                "El proveedor seleccionado no existe o está inactivo."
                        });
                    }

                    idProveedorInicial = idProveedor.Value;
                }

                var token =
                    _escaneoMovilService.CrearSesion(
                        idUsuario.Value,
                        proceso,
                        idProveedorInicial);

                var url =
                    Url.Action(
                        "Escanear",
                        "EscaneoMovil",
                        new { token },
                        Request.Scheme,
                        Request.Host.Value);

                if (string.IsNullOrWhiteSpace(url))
                {
                    _escaneoMovilService.CerrarSesion(token);

                    return Json(new
                    {
                        success = false,
                        message =
                            "No se pudo generar la dirección del escáner."
                    });
                }

                return Json(new
                {
                    success = true,
                    token,
                    proceso,
                    idProveedor = idProveedorInicial,
                    url
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al iniciar una sesión de escaneo para el proceso {Proceso}",
                    proceso);

                return Json(new
                {
                    success = false,
                    message =
                        "No se pudo iniciar la sesión de escaneo."
                });
            }
        }

        [HttpGet]
        public IActionResult EstadoSesion(string token)
        {
            try
            {
                // Primero se comprueba que la sesión exista: una sesión vencida debe informarse
                // como vencida, no como "sin acceso".
                if (string.IsNullOrWhiteSpace(token) ||
                    !_escaneoMovilService.ExisteSesionValida(token))
                {
                    return Json(new
                    {
                        success = false,
                        activa = false,
                        message =
                            "La sesión de escaneo expiró o fue cerrada."
                    });
                }

                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                if (!idUsuario.HasValue || !_escaneoMovilService.EsCreadorSesion(token, idUsuario.Value))
                    return Json(new { success = false, activa = false, message = "No tienes acceso a esta sesión de escaneo." });

                return Json(new
                {
                    success = true,
                    activa = true,
                    proceso =
                        _escaneoMovilService.ObtenerProceso(token),
                    conectado =
                        _escaneoMovilService.EstaCelularConectado(token),
                    idProveedor =
                        _escaneoMovilService.ObtenerIdProveedor(token),
                    tieneEscaneos =
                        _escaneoMovilService.TieneEscaneos(token)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar el estado de la sesión QR");

                return Json(new
                {
                    success = false,
                    activa = false,
                    message =
                        "No se pudo consultar el estado de la sesión."
                });
            }
        }

        [HttpGet]
        public IActionResult ObtenerPendientes(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token) ||
                    !_escaneoMovilService.ExisteSesionValida(token))
                {
                    return Json(new
                    {
                        success = false,
                        message =
                            "La sesión de escaneo expiró o fue cerrada."
                    });
                }

                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                if (!idUsuario.HasValue || !_escaneoMovilService.EsCreadorSesion(token, idUsuario.Value))
                    return Json(new { success = false, message = "No tienes acceso a esta sesión de escaneo." });

                var pendientes =
                    _escaneoMovilService
                        .ObtenerYLimpiarPendientes(token)
                        .Select(item => new
                        {
                            item.IdMaterial,
                            item.IdPaquete,
                            item.Nombre,
                            item.CodigoBarras,
                            item.Categoria,
                            item.UnidadConsumo,
                            item.StockActual,
                            item.Cantidad,
                            item.IdPresentacion,
                            item.Presentacion,
                            item.UnidadPresentacion,
                            item.FactorAUnidadBase,
                            item.FechaEscaneo
                        })
                        .ToList();

                return Json(new
                {
                    success = true,
                    proceso =
                        _escaneoMovilService.ObtenerProceso(token),
                    idProveedor =
                        _escaneoMovilService.ObtenerIdProveedor(token),
                    tieneEscaneos =
                        _escaneoMovilService.TieneEscaneos(token),
                    cantidad = pendientes.Count,
                    pendientes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al consultar los materiales pendientes de una sesión QR");

                return Json(new
                {
                    success = false,
                    message =
                        "No se pudieron obtener los materiales escaneados."
                });
            }
        }

        [HttpPost]
        public IActionResult CerrarSesion(string token)
        {
            try
            {
                // Una sesión que ya venció (o se cerró) cuenta como cerrada: no es un error de acceso.
                if (string.IsNullOrWhiteSpace(token) ||
                    !_escaneoMovilService.ExisteSesionValida(token))
                {
                    return Json(new
                    {
                        success = true,
                        message =
                            "La sesión de escaneo ya estaba cerrada."
                    });
                }

                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                if (!idUsuario.HasValue || !_escaneoMovilService.EsCreadorSesion(token, idUsuario.Value))
                    return Json(new { success = false, message = "No tienes acceso a esta sesión de escaneo." });

                _escaneoMovilService.CerrarSesion(token);

                return Json(new
                {
                    success = true,
                    message =
                        "La sesión de escaneo fue cerrada."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cerrar una sesión QR");

                return Json(new
                {
                    success = false,
                    message =
                        "No se pudo cerrar la sesión de escaneo."
                });
            }
        }
    }
}
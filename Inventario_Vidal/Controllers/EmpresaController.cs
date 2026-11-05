using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using InventarioVidal.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]
    public class EmpresaController : Controller
    {
        private readonly IEmpresaService _empresaService;
        private readonly ITipoCambioService _tipoCambioService;
        private readonly ILogger<EmpresaController> _logger;

        public EmpresaController(
            IEmpresaService empresaService,
            ITipoCambioService tipoCambioService,
            ILogger<EmpresaController> logger)
        {
            _empresaService = empresaService;
            _tipoCambioService = tipoCambioService;
            _logger = logger;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        [RolRequerido("Administrador")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var config = await _empresaService.GetConfiguracionAsync();

                if (config == null)
                    config = CrearConfiguracionInicial();

                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var tipoCambio = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                config.TipoCambio = tipoCambio.Valor;

                ViewBag.TipoCambioFecha = tipoCambio.Fecha;
                ViewBag.TipoCambioCache = tipoCambio.VieneDeCache;
                ViewBag.TipoCambioFallback = tipoCambio.VieneDeFallback;

                return View(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la configuración de empresa");
                TempData["MensajeError"] = "No se pudo cargar la configuración de la empresa.";

                return View(CrearConfiguracionInicial());
            }
        }

        // ============================================================
        // EDITAR - GET
        // ============================================================

        [HttpGet]
        [RolRequerido("Administrador")]
        public async Task<IActionResult> Editar()
        {
            try
            {
                var config = await _empresaService.GetConfiguracionAsync();

                if (config == null)
                    config = CrearConfiguracionInicial();

                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var tipoCambio = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                config.TipoCambio = tipoCambio.Valor;

                ViewBag.TipoCambioFecha = tipoCambio.Fecha;
                ViewBag.TipoCambioCache = tipoCambio.VieneDeCache;
                ViewBag.TipoCambioFallback = tipoCambio.VieneDeFallback;

                return View(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar la configuración de empresa para editar");
                TempData["MensajeError"] = "No se pudo cargar la configuración de la empresa.";

                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // EDITAR - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RolRequerido("Administrador")]
        public async Task<IActionResult> Editar(EmpresaDto model)
        {
            if (!ModelState.IsValid)
            {
                await CargarTipoCambioVistaAsync(model);
                return View(model);
            }

            try
            {
                var result = await _empresaService.UpdateConfiguracionAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["MensajeError"] = result.Message;
                await CargarTipoCambioVistaAsync(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la configuración de empresa");
                TempData["MensajeError"] = "Error al actualizar la configuración.";
                await CargarTipoCambioVistaAsync(model);
                return View(model);
            }
        }

        // ============================================================
        // SUBIR / REEMPLAZAR LOGO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RolRequerido("Administrador")]
        public async Task<IActionResult> UploadLogo(IFormFile? logo)
        {
            if (logo == null || logo.Length == 0)
            {
                TempData["MensajeError"] = "Debe seleccionar un archivo.";
                return RedirectToAction(nameof(Editar));
            }

            try
            {
                var result = await _empresaService.UploadLogoAsync(logo);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    TempData["LogoUrl"] = result.LogoUrl;
                }
                else
                {
                    TempData["MensajeError"] = result.Message;
                }

                return RedirectToAction(nameof(Editar));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir el logo");
                TempData["MensajeError"] = "Error al subir el logo.";

                return RedirectToAction(nameof(Editar));
            }
        }

        // ============================================================
        // ELIMINAR LOGO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RolRequerido("Administrador")]
        public async Task<IActionResult> DeleteLogo()
        {
            try
            {
                var result = await _empresaService.DeleteLogoAsync();

                if (result)
                    TempData["MensajeExito"] = "Logo eliminado correctamente.";
                else
                    TempData["MensajeError"] = "No se pudo eliminar el logo.";

                return RedirectToAction(nameof(Editar));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el logo");
                TempData["MensajeError"] = "Error al eliminar el logo.";

                return RedirectToAction(nameof(Editar));
            }
        }

        // ============================================================
        // TIPO DE CAMBIO ACTUAL
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetTipoCambioActual()
        {
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var tc = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                return Json(new
                {
                    success = true,
                    valor = tc.Valor,
                    fecha = tc.Fecha.ToString("dd/MM/yyyy"),
                    monedaOrigen = "USD",
                    monedaDestino = "PEN",
                    vieneDeCache = tc.VieneDeCache,
                    vieneDeFallback = tc.VieneDeFallback
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el tipo de cambio actual");

                return Json(new
                {
                    success = false,
                    message = "Error al obtener el tipo de cambio."
                });
            }
        }

        // ============================================================
        // VERIFICAR TIPO DE CAMBIO
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> VerificarTipoCambio(
            DateOnly fecha,
            string monedaOrigen = "USD",
            string monedaDestino = "PEN")
        {
            try
            {
                var existe = await _empresaService.ExisteTipoCambioAsync(
                    fecha,
                    monedaOrigen,
                    monedaDestino);

                return Json(new
                {
                    exists = existe
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar tipo de cambio");

                return Json(new
                {
                    exists = false,
                    error = true
                });
            }
        }

        // ============================================================
        // CONFIGURACIÓN PARA REPORTES
        // ============================================================

        [HttpGet]
        [RolRequerido("Administrador")]
        public async Task<IActionResult> GetConfiguracionReporte()
        {
            try
            {
                var config = await _empresaService.GetConfiguracionAsync();

                if (config == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Configuración no encontrada."
                    });
                }

                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var tipoCambio = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        config.NombreEmpresa,
                        config.Ruc,
                        config.Direccion,
                        config.Telefono,
                        config.CorreoElectronico,
                        config.SitioWeb,
                        config.LogoUrl,
                        config.PiePagina,
                        config.Moneda,
                        config.MonedaSecundaria,
                        TipoCambio = tipoCambio.Valor,
                        FechaTipoCambio = tipoCambio.Fecha.ToString("dd/MM/yyyy"),
                        config.Igv
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener configuración de empresa para reportes");

                return Json(new
                {
                    success = false,
                    message = "Error al obtener configuración."
                });
            }
        }

        // ============================================================
        // CONFIGURACIÓN INICIAL
        // ============================================================

        private async Task CargarTipoCambioVistaAsync(EmpresaDto model)
        {
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var tipoCambio = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                model.TipoCambio = tipoCambio.Valor;
                ViewBag.TipoCambioFecha = tipoCambio.Fecha;
                ViewBag.TipoCambioCache = tipoCambio.VieneDeCache;
                ViewBag.TipoCambioFallback = tipoCambio.VieneDeFallback;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo recargar el tipo de cambio al devolver la vista de configuración");
            }
        }

        private static EmpresaDto CrearConfiguracionInicial()
        {
            return new EmpresaDto
            {
                Moneda = "PEN",
                MonedaSecundaria = "USD",
                Igv = 18.00m
            };
        }
    }
}
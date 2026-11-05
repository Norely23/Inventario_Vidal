using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class ProveedoresController : Controller
    {
        private readonly IProveedorService _proveedorService;
        private readonly IMaterialService _materialService;
        private readonly ILogger<ProveedoresController> _logger;

        public ProveedoresController(IProveedorService proveedorService, IMaterialService materialService, ILogger<ProveedoresController> logger)
        {
            _proveedorService = proveedorService;
            _materialService = materialService;
            _logger = logger;
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var proveedores = await _proveedorService.GetProveedoresAsync();
                return View(proveedores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar proveedores");
                TempData["MensajeError"] = "Error al cargar los proveedores.";
                return View(new List<ProveedorListDto>());
            }
        }

        // ============================================================
        // CREAR - GET
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.CREAR")]
        public async Task<IActionResult> Crear()
        {
            try
            {
                await CargarDatosFormularioAsync();

                return View(new ProveedorCreateDto
                {
                    Activo = true,
                    Materiales = new List<ProveedorMaterialPrecioDto>()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar formulario de creación de proveedor");
                TempData["MensajeError"] = "Error al cargar el formulario.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // CREAR - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROVEEDOR.CREAR")]
        public async Task<IActionResult> Crear(ProveedorCreateDto model)
        {
            NormalizarProveedor(model);

            if (!ModelState.IsValid)
            {
                await CargarDatosFormularioAsync();
                return View(model);
            }

            try
            {
                var result = await _proveedorService.CreateProveedorAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["MensajeError"] = result.Message;
                await CargarDatosFormularioAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear proveedor");
                TempData["MensajeError"] = "Error al crear el proveedor.";
                await CargarDatosFormularioAsync();
                return View(model);
            }
        }

        // ============================================================
        // EDITAR - GET
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.EDITAR")]
        public async Task<IActionResult> Editar(int id)
        {
            try
            {
                var proveedor = await _proveedorService.GetProveedorByIdAsync(id);

                if (proveedor == null)
                {
                    TempData["MensajeError"] = "Proveedor no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new ProveedorEditDto
                {
                    IdProveedor = proveedor.IdProveedor,
                    Ruc = proveedor.Ruc,
                    RazonSocial = proveedor.RazonSocial,
                    Direccion = proveedor.Direccion,
                    Telefono = proveedor.Telefono,
                    CorreoElectronico = proveedor.CorreoElectronico,
                    ContactoNombre = proveedor.ContactoNombre,
                    CondicionesPago = proveedor.CondicionesPago,
                    Observaciones = proveedor.Observaciones,
                    Clasificacion = proveedor.Clasificacion,
                    Calificacion = proveedor.Calificacion,
                    Activo = proveedor.Activo,
                    Materiales = MapearMaterialesConPrecios(proveedor)
                };

                await CargarDatosFormularioAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar proveedor para editar. IdProveedor: {IdProveedor}", id);
                TempData["MensajeError"] = "Error al cargar el proveedor.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // EDITAR - POST
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROVEEDOR.EDITAR")]
        public async Task<IActionResult> Editar(ProveedorEditDto model)
        {
            NormalizarProveedor(model);

            if (!ModelState.IsValid)
            {
                await CargarDatosFormularioAsync();
                return View(model);
            }

            try
            {
                var result = await _proveedorService.UpdateProveedorAsync(model);

                if (result.Success)
                {
                    TempData["MensajeExito"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                TempData["MensajeError"] = result.Message;
                await CargarDatosFormularioAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar proveedor. IdProveedor: {IdProveedor}", model.IdProveedor);
                TempData["MensajeError"] = "Error al actualizar el proveedor.";
                await CargarDatosFormularioAsync();
                return View(model);
            }
        }

        // ============================================================
        // DETALLES
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> Detalles(int id)
        {
            try
            {
                var proveedor = await _proveedorService.GetProveedorByIdAsync(id);

                if (proveedor == null)
                {
                    TempData["MensajeError"] = "Proveedor no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                // Se mantiene temporalmente para compatibilidad con la vista actual.
                // El propio ProveedorDetailDto ya contiene ComprasRecientes.
                ViewBag.Compras = proveedor.ComprasRecientes;

                return View(proveedor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar detalles del proveedor. IdProveedor: {IdProveedor}", id);
                TempData["MensajeError"] = "Error al cargar los detalles del proveedor.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // DESACTIVAR - TEMPORAL
        // Se eliminará después de actualizar Index y Detalles.
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.EDITAR")]
        public async Task<IActionResult> Desactivar(int id)
        {
            try
            {
                var proveedor = await _proveedorService.GetProveedorByIdAsync(id);

                if (proveedor == null)
                {
                    TempData["MensajeError"] = "Proveedor no encontrado.";
                    return RedirectToAction(nameof(Index));
                }

                return View(proveedor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar proveedor para cambiar estado. IdProveedor: {IdProveedor}", id);
                TempData["MensajeError"] = "Error al cargar el proveedor.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // ACTIVAR / DESACTIVAR
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("PROVEEDOR.EDITAR")]
        public async Task<IActionResult> ToggleActivo(int id)
        {
            try
            {
                var result = await _proveedorService.ToggleActivoAsync(id);

                if (result.Success)
                    TempData["MensajeExito"] = result.Message;
                else
                    TempData["MensajeError"] = result.Message;

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar estado del proveedor. IdProveedor: {IdProveedor}", id);
                TempData["MensajeError"] = "Error al cambiar el estado del proveedor.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // PROVEEDORES PARA SELECT - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetProveedoresSelect(string? term = null)
        {
            try
            {
                var proveedores = await _proveedorService.GetProveedoresActivosAsync();

                if (!string.IsNullOrWhiteSpace(term))
                {
                    var termino = term.Trim();

                    proveedores = proveedores.Where(p =>
                        p.RazonSocial.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                        p.Ruc.Contains(termino, StringComparison.OrdinalIgnoreCase));
                }

                var result = proveedores
                    .Take(20)
                    .Select(p => new
                    {
                        p.IdProveedor,
                        p.RazonSocial,
                        p.Ruc,
                        display = $"{p.RazonSocial} - {p.Ruc}"
                    });

                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proveedores para select");
                return Json(new { success = false, message = "Error al cargar proveedores." });
            }
        }

        // ============================================================
        // VERIFICAR RUC - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> VerificarRuc(string ruc, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(ruc))
                return Json(new { exists = false, error = false });

            try
            {
                ruc = ruc.Trim();

                var existe = excludeId.HasValue
                    ? await _proveedorService.RucExistsAsync(ruc, excludeId.Value)
                    : await _proveedorService.RucExistsAsync(ruc);

                return Json(new { exists = existe });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar RUC de proveedor: {Ruc}", ruc);
                return Json(new { exists = false, error = true });
            }
        }

        // ============================================================
        // PROVEEDOR POR RUC - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetProveedorByRuc(string ruc)
        {
            if (string.IsNullOrWhiteSpace(ruc))
                return Json(new { success = false, message = "RUC requerido." });

            try
            {
                ruc = ruc.Trim();

                var proveedor = await _proveedorService.GetProveedorByRucAsync(ruc);

                if (proveedor == null)
                    return Json(new { success = false, message = "Proveedor no encontrado." });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        proveedor.IdProveedor,
                        proveedor.RazonSocial,
                        proveedor.Ruc,
                        proveedor.Telefono,
                        proveedor.CorreoElectronico,
                        proveedor.Direccion,
                        proveedor.ContactoNombre
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar proveedor por RUC: {Ruc}", ruc);
                return Json(new { success = false, message = "Error al buscar el proveedor." });
            }
        }

        // ============================================================
        // PROVEEDOR POR ID - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetProveedorById(int id)
        {
            try
            {
                var proveedor = await _proveedorService.GetProveedorByIdAsync(id);

                if (proveedor == null)
                    return Json(new { success = false, message = "Proveedor no encontrado." });

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        proveedor.IdProveedor,
                        proveedor.RazonSocial,
                        proveedor.Ruc,
                        proveedor.Telefono,
                        proveedor.CorreoElectronico,
                        proveedor.Direccion,
                        proveedor.ContactoNombre,
                        proveedor.Clasificacion,
                        proveedor.Calificacion,
                        proveedor.TotalCompras,
                        proveedor.TotalGastado,
                        proveedor.UltimaCompra,
                        preciosVigentes = proveedor.PreciosVigentes.Select(p => new
                        {
                            p.IdMaterialProveedor,
                            p.IdMaterial,
                            p.Material,
                            p.CodigoBarras,
                            p.PrecioMayor,
                            p.PrecioMenor,
                            p.PrecioPromocion,
                            p.Moneda,
                            p.EsProveedorPrincipal
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proveedor por ID {IdProveedor}", id);
                return Json(new { success = false, message = "Error al obtener el proveedor." });
            }
        }

        // ============================================================
        // MATERIALES DEL PROVEEDOR - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetMaterialesByProveedor(int idProveedor)
        {
            try
            {
                var proveedor = await _proveedorService.GetProveedorByIdAsync(idProveedor);

                if (proveedor == null)
                    return Json(new { success = false, message = "Proveedor no encontrado." });

                return Json(new
                {
                    success = true,
                    data = proveedor.PreciosVigentes.Select(m => new
                    {
                        m.IdMaterialProveedor,
                        m.IdMaterial,
                        m.Material,
                        m.CodigoBarras,
                        m.PrecioMayor,
                        m.PrecioMenor,
                        m.PrecioPromocion,
                        m.Moneda,
                        m.EsProveedorPrincipal,
                        m.CodigoProveedor,
                        m.CostoReferenciaUnidadBase,
                        m.UltimoPrecioCompra,
                        m.FechaUltimoPrecio,
                        m.Tarifas,
                        m.PrecioMayorFormateado,
                        m.PrecioMenorFormateado,
                        m.PrecioPromocionFormateado
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener materiales del proveedor {IdProveedor}", idProveedor);
                return Json(new { success = false, message = "Error al cargar materiales." });
            }
        }

        // ============================================================
        // PRECIOS VIGENTES - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetPreciosVigentes(int idProveedor)
        {
            try
            {
                var proveedor = await _proveedorService.GetProveedorByIdAsync(idProveedor);

                if (proveedor == null)
                    return Json(new { success = false, message = "Proveedor no encontrado." });

                return Json(new
                {
                    success = true,
                    data = proveedor.PreciosVigentes.Select(p => new
                    {
                        p.IdMaterialProveedor,
                        p.IdMaterial,
                        p.Material,
                        p.CodigoBarras,
                        p.PrecioMayor,
                        p.PrecioMenor,
                        p.PrecioPromocion,
                        p.Moneda,
                        p.EsProveedorPrincipal,
                        p.CodigoProveedor,
                        p.CostoReferenciaUnidadBase,
                        p.UltimoPrecioCompra,
                        p.FechaUltimoPrecio,
                        p.Tarifas,
                        p.FechaVigencia,
                        FechaVigenciaFormateada = p.FechaVigencia?.ToString("dd/MM/yyyy") ?? "No definida",
                        p.PrecioMayorFormateado,
                        p.PrecioMenorFormateado,
                        p.PrecioPromocionFormateado
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener precios vigentes del proveedor {IdProveedor}", idProveedor);
                return Json(new { success = false, message = "Error al cargar precios." });
            }
        }

        // ============================================================
        // HISTORIAL DE PRECIOS - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetHistorialPrecios(int idProveedor, int? idMaterial = null)
        {
            try
            {
                var proveedor = await _proveedorService.GetProveedorByIdAsync(idProveedor);

                if (proveedor == null)
                    return Json(new { success = false, message = "Proveedor no encontrado." });

                IEnumerable<ProveedorPrecioHistorialDto> historial = proveedor.HistorialPrecios;

                if (idMaterial.HasValue)
                {
                    var relaciones = proveedor.PreciosVigentes
                        .Where(p => p.IdMaterial == idMaterial.Value)
                        .Select(p => p.IdMaterialProveedor)
                        .ToHashSet();

                    historial = historial.Where(h => relaciones.Contains(h.IdMaterialProveedor));
                }

                return Json(new
                {
                    success = true,
                    data = historial.Select(h => new
                    {
                        h.IdPrecioProveedor,
                        h.IdMaterialProveedor,
                        h.Material,
                        h.CodigoBarras,
                        h.IdPresentacion,
                        h.Presentacion,
                        h.FactorAUnidadBase,
                        h.CantidadMinima,
                        h.CantidadMaxima,
                        h.PrecioPresentacion,
                        h.PrecioUnidadBase,
                        h.CostoUnitario,
                        h.Moneda,
                        h.TipoPrecio,
                        h.TipoPrecioDisplay,
                        h.FechaInicioFormateada,
                        h.FechaFinFormateada,
                        h.EstadoBadge,
                        h.EstadoTexto,
                        h.UsuarioRegistro,
                        h.FechaRegistro,
                        h.Observaciones
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial de precios del proveedor {IdProveedor}", idProveedor);
                return Json(new { success = false, message = "Error al cargar el historial." });
            }
        }

        // ============================================================
        // PROVEEDORES DESTACADOS - AJAX
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetProveedoresDestacados(int cantidad = 5)
        {
            try
            {
                cantidad = Math.Clamp(cantidad, 1, 50);

                var proveedores = await _proveedorService.GetTopProveedoresAsync(cantidad);

                return Json(new
                {
                    success = true,
                    data = proveedores.Select(p => new
                    {
                        p.IdProveedor,
                        p.RazonSocial,
                        p.Ruc,
                        p.TotalCompras,
                        p.TotalGastado,
                        p.PorcentajeParticipacion
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener proveedores destacados");
                return Json(new { success = false, message = "Error al cargar proveedores." });
            }
        }

        // ============================================================
        // AJAX - PRESENTACIONES DEL MATERIAL
        // Usado por Crear/Editar proveedor para registrar tarifas
        // por presentación.
        // ============================================================

        [HttpGet]
        [PermisoRequerido("PROVEEDOR.VER")]
        public async Task<IActionResult> GetPresentacionesByMaterial(
            int idMaterial)
        {
            if (idMaterial <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Debe seleccionar un material válido."
                });
            }

            try
            {
                var presentaciones =
                    await _materialService
                        .GetPresentacionesSelectByMaterialAsync(
                            idMaterial);

                var data =
                    presentaciones
                        .Where(p => p.Activo)
                        .OrderByDescending(
                            p => p.EsPredeterminada)
                        .ThenBy(p => p.Nombre)
                        .Select(p => new
                        {
                            p.IdPresentacion,
                            p.Nombre,
                            p.UnidadPresentacion,
                            p.FactorAUnidadBase,
                            p.EsPredeterminada,
                            display =
                                $"{p.Nombre} ({p.UnidadPresentacion}) · factor {p.FactorAUnidadBase:N4}"
                        })
                        .ToList();

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
                    "Error al cargar presentaciones para material {IdMaterial}",
                    idMaterial);

                return Json(new
                {
                    success = false,
                    message =
                        "No se pudieron cargar las presentaciones del material."
                });
            }
        }

        // ============================================================
        // MÉTODOS PRIVADOS
        // ============================================================

        private async Task CargarDatosFormularioAsync()
        {
            ViewBag.Materiales = await _materialService.GetMaterialesActivosAsync();
            ViewBag.Monedas = new List<string> { "PEN", "USD" };
            ViewBag.TiposPrecio = new List<string>
            {
                "Normal",
                "Mayorista",
                "Promocion",
                "Especial"
            };
        }

        private static List<ProveedorMaterialPrecioDto> MapearMaterialesConPrecios(
            ProveedorDetailDto proveedor)
        {
            return proveedor.PreciosVigentes
                .Select(p => new ProveedorMaterialPrecioDto
                {
                    IdMaterial = p.IdMaterial,
                    CodigoProveedor = p.CodigoProveedor,
                    EsProveedorPrincipal = p.EsProveedorPrincipal,
                    Tarifas = p.Tarifas
                        .Select(t => new ProveedorTarifaDto
                        {
                            IdPrecioProveedor = t.IdPrecioProveedor,
                            IdPresentacion = t.IdPresentacion,
                            Presentacion = t.Presentacion,
                            FactorAUnidadBase = t.FactorAUnidadBase,
                            TipoPrecio = t.TipoPrecio,
                            CantidadMinima = t.CantidadMinima,
                            CantidadMaxima = t.CantidadMaxima,
                            PrecioPresentacion = t.PrecioPresentacion,
                            Moneda = t.Moneda,
                            FechaInicio = t.FechaInicio,
                            FechaFin = t.FechaFin,
                            Observaciones = t.Observaciones
                        })
                        .ToList(),
                    PrecioMayor = p.PrecioMayor,
                    PrecioMenor = p.PrecioMenor,
                    PrecioPromocion = p.PrecioPromocion,
                    Moneda = string.IsNullOrWhiteSpace(p.Moneda)
                        ? "PEN"
                        : p.Moneda.Trim().ToUpperInvariant(),
                    FechaVigencia = p.FechaVigencia
                })
                .ToList();
        }

        private static void NormalizarProveedor(ProveedorCreateDto model)
        {
            model.Ruc = model.Ruc?.Trim() ?? string.Empty;
            model.RazonSocial = model.RazonSocial?.Trim() ?? string.Empty;
            model.Direccion = LimpiarTexto(model.Direccion);
            model.Telefono = LimpiarTexto(model.Telefono);
            model.CorreoElectronico = LimpiarTexto(model.CorreoElectronico);
            model.ContactoNombre = LimpiarTexto(model.ContactoNombre);
            model.CondicionesPago = LimpiarTexto(model.CondicionesPago);
            model.Observaciones = LimpiarTexto(model.Observaciones);
            model.Clasificacion = LimpiarTexto(model.Clasificacion);
            NormalizarMateriales(model.Materiales);
        }

        private static void NormalizarProveedor(ProveedorEditDto model)
        {
            model.Ruc = model.Ruc?.Trim() ?? string.Empty;
            model.RazonSocial = model.RazonSocial?.Trim() ?? string.Empty;
            model.Direccion = LimpiarTexto(model.Direccion);
            model.Telefono = LimpiarTexto(model.Telefono);
            model.CorreoElectronico = LimpiarTexto(model.CorreoElectronico);
            model.ContactoNombre = LimpiarTexto(model.ContactoNombre);
            model.CondicionesPago = LimpiarTexto(model.CondicionesPago);
            model.Observaciones = LimpiarTexto(model.Observaciones);
            model.Clasificacion = LimpiarTexto(model.Clasificacion);
            NormalizarMateriales(model.Materiales);
        }

        private static void NormalizarMateriales(
            List<ProveedorMaterialPrecioDto>? materiales)
        {
            if (materiales == null)
                return;

            foreach (var material in materiales)
            {
                material.CodigoProveedor = LimpiarTexto(material.CodigoProveedor);
                material.Moneda = string.IsNullOrWhiteSpace(material.Moneda)
                    ? "PEN"
                    : material.Moneda.Trim().ToUpperInvariant();

                material.Observaciones = LimpiarTexto(material.Observaciones);
                material.Tarifas ??= new List<ProveedorTarifaDto>();

                foreach (var tarifa in material.Tarifas)
                {
                    tarifa.TipoPrecio = string.IsNullOrWhiteSpace(tarifa.TipoPrecio)
                        ? "Normal"
                        : tarifa.TipoPrecio.Trim();

                    tarifa.Moneda = string.IsNullOrWhiteSpace(tarifa.Moneda)
                        ? material.Moneda
                        : tarifa.Moneda.Trim().ToUpperInvariant();

                    tarifa.Observaciones = LimpiarTexto(tarifa.Observaciones);
                }
            }
        }

        private static string? LimpiarTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers
{
    [LoginRequerido]

    public class MaterialesController : Controller
    {
        private readonly IMaterialService _materialService;
        private readonly ICategoriaService _categoriaService;
        private readonly IMarcaService _marcaService;
        private readonly IUnidadMedidaService _unidadMedidaService;
        private readonly IKardexService _kardexService;
        private readonly IImagenUploadService _imagenUploadService;
        private readonly ILogger<MaterialesController> _logger;
        private readonly FabricacionesVidalEirlBdContext _context;

        public MaterialesController(
            IMaterialService materialService,
            ICategoriaService categoriaService,
            IMarcaService marcaService,
            IUnidadMedidaService unidadMedidaService,
            IKardexService kardexService,
            IImagenUploadService imagenUploadService,
            ILogger<MaterialesController> logger,
            FabricacionesVidalEirlBdContext context)
        {
            _materialService = materialService;
            _categoriaService = categoriaService;
            _marcaService = marcaService;
            _unidadMedidaService = unidadMedidaService;
            _kardexService = kardexService;
            _imagenUploadService = imagenUploadService;
            _logger = logger;
            _context = context;
        }

        // ================================================================
        // INDEX - LISTADO DE MATERIALES
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> Index(MaterialFiltroDto? filtro = null)
        {
            try
            {
                IEnumerable<MaterialListDto> materiales;

                if (filtro?.SoloStockBajo == true)
                {
                    materiales =
                        await _materialService
                            .GetMaterialesStockBajoAsync();
                }
                else if (filtro?.SoloActivos == true)
                {
                    materiales =
                        await _materialService
                            .GetMaterialesActivosAsync();
                }
                else
                {
                    materiales =
                        await _materialService
                            .GetMaterialesAsync();
                }

                if (!string.IsNullOrEmpty(filtro?.Buscar))
                {
                    var busqueda =
                        filtro.Buscar.ToLower();

                    materiales =
                        materiales.Where(
                            m =>
                                m.Nombre
                                    .ToLower()
                                    .Contains(busqueda)
                                ||
                                (
                                    m.CodigoBarras != null
                                    &&
                                    m.CodigoBarras
                                        .ToLower()
                                        .Contains(busqueda)
                                )
                                ||
                                m.Categoria
                                    .ToLower()
                                    .Contains(busqueda)
                        );
                }

                if (
                    filtro?.IdCategoria.HasValue == true
                    &&
                    filtro.IdCategoria.Value > 0
                )
                {
                    materiales =
                        materiales.Where(
                            m =>
                                m.IdCategoria
                                ==
                                filtro.IdCategoria.Value
                        );
                }

                // ============================================================
                // PRESENTACIÓN PRINCIPAL POR MATERIAL
                // ============================================================

                var materialesIds =
                    materiales
                        .Select(m => m.IdMaterial)
                        .ToList();

                var presentacionesDict =
                    new Dictionary<
                        int,
                        PresentacionMaterialDto
                    >();

                if (materialesIds.Any())
                {
                    var presentaciones =
                        await _context
                            .PresentacionesMaterial
                            .Include(
                                p =>
                                    p.IdUnidadPresentacionNavigation
                            )
                            .Where(
                                p =>
                                    materialesIds.Contains(
                                        p.IdMaterial
                                    )
                                    &&
                                    p.EsPredeterminada
                                    &&
                                    p.Activo
                            )
                            .Select(
                                p =>
                                    new PresentacionMaterialDto
                                    {
                                        IdMaterial =
                                            p.IdMaterial,

                                        IdPresentacion =
                                            p.IdPresentacion,

                                        Nombre =
                                            p.Nombre,

                                        UnidadPresentacion =
                                            p.IdUnidadPresentacionNavigation
                                                .Nombre,

                                        AbreviaturaUnidad =
                                            p.IdUnidadPresentacionNavigation
                                                .Abreviatura,

                                        FactorAUnidadBase =
                                            p.FactorAUnidadBase,

                                        EsPredeterminada =
                                            p.EsPredeterminada,

                                        Activo =
                                            p.Activo
                                    }
                            )
                            .ToListAsync();

                    var materialesSinPredeterminada =
                        materialesIds
                            .Where(
                                id =>
                                    !presentaciones.Any(
                                        p =>
                                            p.IdMaterial == id
                                    )
                            )
                            .ToList();

                    if (materialesSinPredeterminada.Any())
                    {
                        var presentacionesFallback =
                            await _context
                                .PresentacionesMaterial
                                .Include(
                                    p =>
                                        p.IdUnidadPresentacionNavigation
                                )
                                .Where(
                                    p =>
                                        materialesSinPredeterminada
                                            .Contains(
                                                p.IdMaterial
                                            )
                                        &&
                                        p.Activo
                                )
                                .Select(
                                    p =>
                                        new PresentacionMaterialDto
                                        {
                                            IdMaterial =
                                                p.IdMaterial,

                                            IdPresentacion =
                                                p.IdPresentacion,

                                            Nombre =
                                                p.Nombre,

                                            UnidadPresentacion =
                                                p.IdUnidadPresentacionNavigation
                                                    .Nombre,

                                            AbreviaturaUnidad =
                                                p.IdUnidadPresentacionNavigation
                                                    .Abreviatura,

                                            FactorAUnidadBase =
                                                p.FactorAUnidadBase,

                                            EsPredeterminada =
                                                p.EsPredeterminada,

                                            Activo =
                                                p.Activo
                                        }
                                )
                                .ToListAsync();

                        presentaciones.AddRange(
                            presentacionesFallback
                        );
                    }

                    foreach (var id in materialesIds)
                    {
                        if (
                            !presentaciones.Any(
                                p =>
                                    p.IdMaterial == id
                            )
                        )
                        {
                            presentaciones.Add(
                                new PresentacionMaterialDto
                                {
                                    IdMaterial =
                                        id,

                                    Nombre =
                                        "Sin presentación",

                                    UnidadPresentacion =
                                        "N/A",

                                    FactorAUnidadBase =
                                        1,

                                    EsPredeterminada =
                                        false,

                                    Activo =
                                        false
                                }
                            );
                        }
                    }

                    presentacionesDict =
                        presentaciones
                            .GroupBy(
                                p =>
                                    p.IdMaterial
                            )
                            .Select(
                                g =>
                                    g.First()
                            )
                            .ToDictionary(
                                p =>
                                    p.IdMaterial,
                                p =>
                                    p
                            );
                }

                ViewBag.PresentacionesDict =
                    presentacionesDict;

                // ============================================================
                // FILTRO POR MARCA
                // ============================================================

                if (
                    filtro?.IdMarca.HasValue == true
                    &&
                    filtro.IdMarca.Value > 0
                )
                {
                    var marca =
                        await _marcaService
                            .GetMarcaByIdAsync(
                                filtro.IdMarca.Value
                            );

                    if (marca != null)
                    {
                        materiales =
                            materiales.Where(
                                m =>
                                    m.Marca == marca.Nombre
                            );
                    }
                }

                ViewBag.Categorias =
                    await _categoriaService
                        .GetCategoriasActivasAsync();

                ViewBag.Marcas =
                    await _marcaService
                        .GetMarcasActivasAsync();

                ViewBag.Filtro =
                    filtro
                    ??
                    new MaterialFiltroDto();

                return View(materiales);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar materiales"
                );

                TempData["MensajeError"] =
                    "Error al cargar los materiales";

                return View(
                    new List<MaterialListDto>()
                );
            }
        }

        // ================================================================
        // CREAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.CREAR")]
        public async Task<IActionResult> Crear()
        {
            try
            {
                ViewBag.Categorias =
                    await _categoriaService
                        .GetCategoriasActivasAsync();

                ViewBag.Marcas =
                    await _marcaService
                        .GetMarcasActivasAsync();

                ViewBag.UnidadesCompra =
                    await _unidadMedidaService
                        .GetUnidadesSelectAsync();

                ViewBag.UnidadesConsumo =
                    await _unidadMedidaService
                        .GetUnidadesSelectAsync();

                return View(
                    new MaterialCreateDto
                    {
                        EsReutilizable =
                            false,

                        EsHerramienta =
                            false,

                        StockMinimo =
                            0,

                        Presentaciones =
                            new List<
                                PresentacionMaterialCreateDto
                            >()
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar formulario de creación de material"
                );

                TempData["MensajeError"] =
                    "Error al cargar el formulario";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // CREAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.CREAR")]
        public async Task<IActionResult> Crear(
            MaterialCreateDto model)
        {
            if (!ModelState.IsValid)
            {
                await CargarCatalogosMaterialAsync();

                return View(model);
            }

            var subida =
                await _imagenUploadService
                    .SubirImagenAsync(
                        model.ImagenArchivo,
                        "materiales"
                    );

            if (!subida.Success)
            {
                ModelState.AddModelError(
                    nameof(model.ImagenArchivo),
                    subida.Message
                );

                await CargarCatalogosMaterialAsync();

                return View(model);
            }

            model.ImagenUrl =
                subida.RutaRelativa;

            try
            {
                var usuarioId =
                    ObtenerUsuarioId();

                if (usuarioId <= 0)
                {
                    TempData["MensajeError"] =
                        "No se pudo identificar al usuario de la sesión";

                    await CargarCatalogosMaterialAsync();

                    return View(model);
                }

                var result =
                    await _materialService
                        .CreateMaterialAsync(
                            model,
                            usuarioId
                        );

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;

                    return RedirectToAction(
                        nameof(Detalles),
                        new
                        {
                            id =
                                result.IdMaterial
                        }
                    );
                }

                TempData["MensajeError"] =
                    result.Message;

                await CargarCatalogosMaterialAsync();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear material"
                );

                TempData["MensajeError"] =
                    "Error al crear el material";

                await CargarCatalogosMaterialAsync();

                return View(model);
            }
        }

        // ================================================================
        // EDITAR - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.EDITAR")]
        public async Task<IActionResult> Editar(
            int id)
        {
            try
            {
                var material =
                    await _materialService
                        .GetMaterialByIdAsync(id);

                if (material == null)
                {
                    TempData["MensajeError"] =
                        "Material no encontrado";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                await CargarCatalogosMaterialAsync();

                ViewBag.Presentaciones =
                    await _materialService
                        .GetPresentacionesAllByMaterialAsync(
                            id);

                var model =
                    new MaterialEditDto
                    {
                        IdMaterial =
                            material.IdMaterial,

                        CodigoBarras =
                            material.CodigoBarras,

                        Nombre =
                            material.Nombre,

                        Descripcion =
                            material.Descripcion,

                        IdCategoria =
                            material.IdCategoria,

                        IdMarca =
                            material.IdMarca,

                        IdUnidadConsumo =
                            material.IdUnidadConsumo,

                        EsReutilizable =
                            material.EsReutilizable,

                        EsHerramienta =
                            material.EsHerramienta,

                        StockMinimo =
                            material.StockMinimo,

                        ImagenUrl =
                            material.ImagenUrl,

                        Activo =
                            material.Activo
                    };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar material para editar"
                );

                TempData["MensajeError"] =
                    "Error al cargar el material";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // EDITAR - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.EDITAR")]
        public async Task<IActionResult> Editar(
            MaterialEditDto model)
        {
            if (!ModelState.IsValid)
            {
                await CargarCatalogosMaterialAsync(model.IdMaterial);

                return View(model);
            }

            var subida =
                await _imagenUploadService
                    .SubirImagenAsync(
                        model.ImagenArchivo,
                        "materiales",
                        model.ImagenUrl
                    );

            if (!subida.Success)
            {
                ModelState.AddModelError(
                    nameof(model.ImagenArchivo),
                    subida.Message
                );

                await CargarCatalogosMaterialAsync(model.IdMaterial);

                return View(model);
            }

            model.ImagenUrl =
                subida.RutaRelativa;

            try
            {
                var usuarioId =
                    ObtenerUsuarioId();

                if (usuarioId <= 0)
                {
                    TempData["MensajeError"] =
                        "No se pudo identificar al usuario de la sesión";

                    await CargarCatalogosMaterialAsync(
                        model.IdMaterial);

                    return View(model);
                }

                var result =
                    await _materialService
                        .UpdateMaterialAsync(
                            model,
                            usuarioId);

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;

                    return RedirectToAction(
                        nameof(Detalles),
                        new
                        {
                            id =
                                model.IdMaterial
                        }
                    );
                }

                TempData["MensajeError"] =
                    result.Message;

                await CargarCatalogosMaterialAsync(model.IdMaterial);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar material"
                );

                TempData["MensajeError"] =
                    "Error al actualizar el material";

                await CargarCatalogosMaterialAsync(model.IdMaterial);

                return View(model);
            }
        }

        // ================================================================
        // DETALLES
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> Detalles(
            int id)
        {
            try
            {
                var material =
                    await _materialService
                        .GetMaterialByIdAsync(id);

                if (material == null)
                {
                    TempData["MensajeError"] =
                        "Material no encontrado";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                var movimientos =
                    await _kardexService
                        .GetMovimientosByMaterialAsync(
                            id,
                            DateTime.Now.AddMonths(-6),
                            DateTime.Now
                        );

                ViewBag.Movimientos =
                    movimientos
                        .OrderByDescending(
                            m =>
                                m.FechaMovimiento
                        )
                        .Take(20);

                return View(material);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar detalles del material"
                );

                TempData["MensajeError"] =
                    "Error al cargar los detalles";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // ACTIVAR / DESACTIVAR MATERIAL
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.ELIMINAR")]
        public async Task<IActionResult> ToggleActivo(
            int id)
        {
            try
            {
                var result =
                    await _materialService
                        .ToggleActivoAsync(id);

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
                    nameof(Index)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar estado del material {Id}",
                    id
                );

                TempData["MensajeError"] =
                    "Error al cambiar el estado del material";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // ESCÁNER DE RECEPCIÓN - RUTA LEGACY
        // ================================================================
        //
        // El flujo oficial de recepción es:
        // Compras -> Registrar -> Escanear materiales -> QR -> celular.
        //
        // Esta ruta se conserva únicamente para enlaces antiguos.
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public IActionResult Escanear()
        {
            return RedirectToAction(
                "Registrar",
                "Compras");
        }

        // ================================================================
        // ETIQUETA INDIVIDUAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> EtiquetaIndividual(
            int id)
        {
            try
            {
                var material =
                    await _materialService
                        .GetMaterialByIdAsync(id);

                if (material == null)
                {
                    TempData["MensajeError"] =
                        "Material no encontrado";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                return View(material);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar etiqueta individual del material {Id}",
                    id
                );

                TempData["MensajeError"] =
                    "Error al cargar la etiqueta";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // ETIQUETAS MÚLTIPLES
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> EtiquetasMultiples(
            string ids)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ids))
                {
                    TempData["MensajeError"] =
                        "No se seleccionaron materiales";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                var idList =
                    ids.Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries
                        )
                        .Select(
                            value =>
                                int.TryParse(
                                    value,
                                    out var id
                                )
                                    ? id
                                    : 0
                        )
                        .Where(
                            id =>
                                id > 0
                        )
                        .Distinct()
                        .ToList();

                if (!idList.Any())
                {
                    TempData["MensajeError"] =
                        "No se seleccionaron materiales válidos";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                var materiales =
                    new List<
                        MaterialDetailDto
                    >();

                foreach (var id in idList)
                {
                    var material =
                        await _materialService
                            .GetMaterialByIdAsync(id);

                    if (material != null)
                    {
                        materiales.Add(
                            material
                        );
                    }
                }

                if (!materiales.Any())
                {
                    TempData["MensajeError"] =
                        "No se encontraron materiales";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                return View(materiales);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar etiquetas múltiples"
                );

                TempData["MensajeError"] =
                    "Error al cargar las etiquetas";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // QR PARA ACCESO AL ESCÁNER
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public IActionResult QrAccesoScanner()
        {
            return RedirectToAction(
                "Registrar",
                "Compras");
        }

        // ================================================================
        // AJAX - MATERIALES PARA SELECT
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
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
                                        $"{m.Nombre} ({m.CodigoBarras}) - Stock: {m.StockActual}"
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
        // AJAX - MATERIALES POR CATEGORÍA
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> GetMaterialesByCategoria(
            int idCategoria)
        {
            try
            {
                var materiales =
                    await _materialService
                        .GetMaterialesByCategoriaAsync(
                            idCategoria
                        );

                var result =
                    materiales.Select(
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
                    );

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
                    "Error al obtener materiales por categoría"
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
        // AJAX - MATERIALES CON STOCK BAJO
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> GetMaterialesStockBajo()
        {
            try
            {
                var materiales =
                    await _materialService
                        .GetMaterialesStockBajoAsync();

                return Json(
                    new
                    {
                        success =
                            true,

                        total =
                            materiales.Count(),

                        data =
                            materiales.Select(
                                m =>
                                    new
                                    {
                                        m.IdMaterial,
                                        m.Nombre,
                                        m.CodigoBarras,
                                        m.Categoria,
                                        m.StockActual,
                                        m.StockMinimo,
                                        m.ImagenUrl
                                    }
                            )
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener materiales con stock bajo"
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
        // AJAX - VERIFICAR CÓDIGO DE BARRAS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.CREAR")]
        public async Task<IActionResult> VerificarCodigoBarras(
            string codigoBarras,
            int? excludeId = null)
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(
                        codigoBarras
                    )
                )
                {
                    return Json(
                        new
                        {
                            exists =
                                false,

                            error =
                                true,

                            message =
                                "Código de barras requerido"
                        }
                    );
                }

                bool existe;

                if (excludeId.HasValue)
                {
                    existe =
                        await _materialService
                            .CodigoBarrasExistsAsync(
                                codigoBarras,
                                excludeId.Value
                            );
                }
                else
                {
                    existe =
                        await _materialService
                            .CodigoBarrasExistsAsync(
                                codigoBarras
                            );
                }

                return Json(
                    new
                    {
                        exists =
                            existe
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al verificar código de barras"
                );

                return Json(
                    new
                    {
                        exists =
                            false,

                        error =
                            true
                    }
                );
            }
        }

        // ================================================================
        // AJAX - GENERAR CÓDIGO DE BARRAS
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.CREAR")]
        public async Task<IActionResult> GenerarCodigoBarras(
            int idCategoria)
        {
            try
            {
                var codigo =
                    await _materialService
                        .GenerarCodigoBarrasAsync(
                            idCategoria
                        );

                return Json(
                    new
                    {
                        success =
                            true,

                        codigo =
                            codigo,

                        codigoBarras =
                            codigo
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al generar código de barras"
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        message =
                            "Error al generar código"
                    }
                );
            }
        }

        // ================================================================
        // PRESENTACIONES
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> Presentaciones(
            int id)
        {
            try
            {
                var material =
                    await _materialService
                        .GetMaterialByIdAsync(id);

                if (material == null)
                {
                    TempData["MensajeError"] =
                        "Material no encontrado";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                var presentaciones =
                    await _materialService
                        .GetPresentacionesAllByMaterialAsync(
                            id
                        );

                ViewBag.Material =
                    material;

                return View(
                    "Presentaciones/Index",
                    presentaciones
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar presentaciones del material {Id}",
                    id
                );

                TempData["MensajeError"] =
                    "Error al cargar las presentaciones";

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
        // CREAR PRESENTACIÓN - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.CREAR")]
        public async Task<IActionResult> CrearPresentacion(
            int idMaterial)
        {
            try
            {
                var material =
                    await _materialService
                        .GetMaterialByIdAsync(
                            idMaterial
                        );

                if (material == null)
                {
                    TempData["MensajeError"] =
                        "Material no encontrado";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                ViewBag.Material =
                    material;

                ViewBag.Unidades =
                    await _unidadMedidaService
                        .GetUnidadesSelectAsync();

                return View(
                    "Presentaciones/Crear",
                    new PresentacionMaterialCreateDto
                    {
                        IdMaterial =
                            idMaterial,

                        EsPredeterminada =
                            false
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar formulario de creación de presentación"
                );

                TempData["MensajeError"] =
                    "Error al cargar el formulario";

                return RedirectToAction(
                    nameof(Detalles),
                    new
                    {
                        id =
                            idMaterial
                    }
                );
            }
        }

        // ================================================================
        // CREAR PRESENTACIÓN - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.CREAR")]
        public async Task<IActionResult> CrearPresentacion(
            int idMaterial,
            PresentacionMaterialCreateDto model)
        {
            model.IdMaterial =
                idMaterial;

            if (!ModelState.IsValid)
            {
                await CargarFormularioPresentacionAsync(
                    idMaterial
                );

                return View(
                    "Presentaciones/Crear",
                    model
                );
            }

            try
            {
                var usuarioId =
                    ObtenerUsuarioId();

                if (usuarioId <= 0)
                {
                    TempData["MensajeError"] =
                        "No se pudo identificar al usuario de la sesión";

                    await CargarFormularioPresentacionAsync(
                        idMaterial
                    );

                    return View(
                        "Presentaciones/Crear",
                        model
                    );
                }

                var result =
                    await _materialService
                        .CreatePresentacionAsync(
                            idMaterial,
                            model,
                            usuarioId
                        );

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;

                    return RedirectToAction(
                        nameof(Presentaciones),
                        new
                        {
                            id =
                                idMaterial
                        }
                    );
                }

                TempData["MensajeError"] =
                    result.Message;

                await CargarFormularioPresentacionAsync(
                    idMaterial
                );

                return View(
                    "Presentaciones/Crear",
                    model
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al crear presentación"
                );

                TempData["MensajeError"] =
                    "Error al crear la presentación";

                await CargarFormularioPresentacionAsync(
                    idMaterial
                );

                return View(
                    "Presentaciones/Crear",
                    model
                );
            }
        }

        // ================================================================
        // EDITAR PRESENTACIÓN - GET
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.EDITAR")]
        public async Task<IActionResult> EditarPresentacion(
            int id)
        {
            try
            {
                var presentacion =
                    await _materialService
                        .GetPresentacionByIdAsync(id);

                if (presentacion == null)
                {
                    TempData["MensajeError"] =
                        "Presentación no encontrada";

                    return RedirectToAction(
                        nameof(Index)
                    );
                }

                var material =
                    await _materialService
                        .GetMaterialByIdAsync(
                            presentacion.IdMaterial
                        );

                ViewBag.Material =
                    material;

                ViewBag.Unidades =
                    await _unidadMedidaService
                        .GetUnidadesSelectAsync();

                var model =
                    new PresentacionMaterialEditDto
                    {
                        IdPresentacion =
                            presentacion.IdPresentacion,

                        IdMaterial =
                            presentacion.IdMaterial,

                        Nombre =
                            presentacion.Nombre,

                        IdUnidadPresentacion =
                            presentacion.IdUnidadPresentacion,

                        FactorAUnidadBase =
                            presentacion.FactorAUnidadBase,

                        EsPredeterminada =
                            presentacion.EsPredeterminada,

                        Activo =
                            presentacion.Activo
                    };

                return View(
                    "Presentaciones/Editar",
                    model
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cargar presentación para editar"
                );

                TempData["MensajeError"] =
                    "Error al cargar la presentación";

                return RedirectToAction(
                    nameof(Index)
                );
            }
        }

        // ================================================================
        // EDITAR PRESENTACIÓN - POST
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.EDITAR")]
        public async Task<IActionResult> EditarPresentacion(
            PresentacionMaterialEditDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Material =
                    await _materialService
                        .GetMaterialByIdAsync(
                            model.IdMaterial
                        );

                ViewBag.Unidades =
                    await _unidadMedidaService
                        .GetUnidadesSelectAsync();

                return View(
                    "Presentaciones/Editar",
                    model
                );
            }

            try
            {
                var result =
                    await _materialService
                        .UpdatePresentacionAsync(
                            model
                        );

                if (result.Success)
                {
                    TempData["MensajeExito"] =
                        result.Message;

                    return RedirectToAction(
                        nameof(Presentaciones),
                        new
                        {
                            id =
                                model.IdMaterial
                        }
                    );
                }

                TempData["MensajeError"] =
                    result.Message;

                ViewBag.Material =
                    await _materialService
                        .GetMaterialByIdAsync(
                            model.IdMaterial
                        );

                ViewBag.Unidades =
                    await _unidadMedidaService
                        .GetUnidadesSelectAsync();

                return View(
                    "Presentaciones/Editar",
                    model
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al actualizar presentación"
                );

                TempData["MensajeError"] =
                    "Error al actualizar la presentación";

                ViewBag.Material =
                    await _materialService
                        .GetMaterialByIdAsync(
                            model.IdMaterial
                        );

                ViewBag.Unidades =
                    await _unidadMedidaService
                        .GetUnidadesSelectAsync();

                return View(
                    "Presentaciones/Editar",
                    model
                );
            }
        }

        // ================================================================
        // ESTABLECER PRESENTACIÓN PREDETERMINADA
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.EDITAR")]
        public async Task<IActionResult> SetPresentacionDefault(
            int idPresentacion,
            int idMaterial)
        {
            try
            {
                var result =
                    await _materialService
                        .SetPresentacionDefaultAsync(
                            idPresentacion,
                            idMaterial
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
                    nameof(Presentaciones),
                    new
                    {
                        id =
                            idMaterial
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al establecer presentación predeterminada"
                );

                TempData["MensajeError"] =
                    "Error al cambiar la presentación predeterminada";

                return RedirectToAction(
                    nameof(Presentaciones),
                    new
                    {
                        id =
                            idMaterial
                    }
                );
            }
        }

        // ================================================================
        // ACTIVAR / DESACTIVAR PRESENTACIÓN
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.EDITAR")]
        public async Task<IActionResult> TogglePresentacionActivo(
            int idPresentacion,
            int idMaterial)
        {
            try
            {
                var result =
                    await _materialService
                        .TogglePresentacionActivoAsync(
                            idPresentacion
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
                    nameof(Presentaciones),
                    new
                    {
                        id =
                            idMaterial
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al cambiar estado de la presentación"
                );

                TempData["MensajeError"] =
                    "Error al cambiar el estado de la presentación";

                return RedirectToAction(
                    nameof(Presentaciones),
                    new
                    {
                        id =
                            idMaterial
                    }
                );
            }
        }

        // ================================================================
        // ELIMINAR PRESENTACIÓN
        // ================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [PermisoRequerido("MATERIAL.EDITAR")]
        public async Task<IActionResult> EliminarPresentacion(
            int idPresentacion,
            int idMaterial)
        {
            try
            {
                var result =
                    await _materialService
                        .DeletePresentacionAsync(
                            idPresentacion
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
                    nameof(Presentaciones),
                    new
                    {
                        id =
                            idMaterial
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al eliminar presentación"
                );

                TempData["MensajeError"] =
                    "Error al eliminar la presentación";

                return RedirectToAction(
                    nameof(Presentaciones),
                    new
                    {
                        id =
                            idMaterial
                    }
                );
            }
        }

        // ================================================================
        // AJAX - PRESENTACIONES POR MATERIAL
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> GetPresentacionesByMaterial(
            int idMaterial)
        {
            if (idMaterial <= 0)
            {
                return Json(
                    new
                    {
                        success = false,
                        data = Array.Empty<object>(),
                        message =
                            "Debe seleccionar un material válido"
                    });
            }

            try
            {
                var presentaciones =
                    await _materialService
                        .GetPresentacionesSelectByMaterialAsync(
                            idMaterial
                        );

                var data =
                    presentaciones
                        .Where(
                            p =>
                                p.Activo
                        )
                        .OrderByDescending(
                            p =>
                                p.EsPredeterminada
                        )
                        .ThenBy(
                            p =>
                                p.Nombre
                        )
                        .Select(
                            p =>
                                new
                                {
                                    p.IdPresentacion,
                                    p.Nombre,
                                    p.UnidadPresentacion,
                                    p.FactorAUnidadBase,
                                    p.EsPredeterminada,
                                    p.Activo,

                                    display =
                                        $"{p.Nombre} ({p.UnidadPresentacion}) - 1 = {p.FactorAUnidadBase} unidades base"
                                }
                        )
                        .ToList();

                return Json(
                    new
                    {
                        success =
                            true,

                        data
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener presentaciones del material {IdMaterial}",
                    idMaterial
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        message =
                            "Error al cargar presentaciones"
                    }
                );
            }
        }

        // ================================================================
        // AJAX - FACTOR DE CONVERSIÓN
        // ================================================================

        [HttpGet]
        [PermisoRequerido("MATERIAL.VER")]
        public async Task<IActionResult> GetFactorConversionVigente(
            int idMaterial)
        {
            try
            {
                var factor =
                    await _materialService
                        .GetFactorConversionVigenteAsync(
                            idMaterial
                        );

                return Json(
                    new
                    {
                        success =
                            true,

                        data =
                            new
                            {
                                factor.FactorConversion,
                                factor.Origen,
                                factor.IdPresentacionUsada,
                                factor.PresentacionNombre,
                                factor.UnidadBase
                            }
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al obtener factor de conversión vigente del material {IdMaterial}",
                    idMaterial
                );

                return Json(
                    new
                    {
                        success =
                            false,

                        message =
                            "Error al obtener factor de conversión"
                    }
                );
            }
        }

        // ================================================================
        // MÉTODOS PRIVADOS
        // ================================================================

        private int ObtenerUsuarioId()
        {
            return HttpContext.Session
                .GetInt32("IdUsuario")
                ?? 0;
        }

        private async Task CargarCatalogosMaterialAsync(
            int? idMaterial = null)
        {
            ViewBag.Categorias =
                await _categoriaService
                    .GetCategoriasActivasAsync();

            ViewBag.Marcas =
                await _marcaService
                    .GetMarcasActivasAsync();

            var unidades =
                await _unidadMedidaService
                    .GetUnidadesSelectAsync();

            ViewBag.UnidadesCompra =
                unidades;

            ViewBag.UnidadesConsumo =
                unidades;

            if (
                idMaterial.HasValue &&
                idMaterial.Value > 0)
            {
                ViewBag.Presentaciones =
                    await _materialService
                        .GetPresentacionesAllByMaterialAsync(
                            idMaterial.Value);
            }
        }

        private async Task CargarFormularioPresentacionAsync(
            int idMaterial)
        {
            ViewBag.Material =
                await _materialService
                    .GetMaterialByIdAsync(
                        idMaterial
                    );

            ViewBag.Unidades =
                await _unidadMedidaService
                    .GetUnidadesSelectAsync();
        }

    }
}
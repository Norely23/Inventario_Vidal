using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Inventario_Vidal.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EmpresaService> _logger;
        private readonly IAuditoriaService _auditoriaService;

        public EmpresaService(
            FabricacionesVidalEirlBdContext context,
            IWebHostEnvironment env,
            ILogger<EmpresaService> logger,
            IAuditoriaService auditoriaService)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _auditoriaService = auditoriaService;
        }

        // ============================================================
        // CONFIGURACIÓN DE EMPRESA
        // ============================================================

        public async Task<EmpresaDto?> GetConfiguracionAsync()
        {
            try
            {
                var config = await _context.ConfiguracionEmpresas
                    .AsNoTracking()
                    .OrderBy(x => x.IdConfiguracion)
                    .FirstOrDefaultAsync();

                if (config == null)
                    return null;

                var tipoCambioActual = await _context.TiposCambios
                    .AsNoTracking()
                    .Where(t => t.MonedaOrigen == "USD" && t.MonedaDestino == "PEN")
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.FechaRegistro)
                    .Select(t => (decimal?)t.Valor)
                    .FirstOrDefaultAsync() ?? 3.80m;

                return new EmpresaDto
                {
                    IdConfiguracion = config.IdConfiguracion,
                    NombreEmpresa = config.NombreEmpresa,
                    Ruc = config.Ruc,
                    Direccion = config.Direccion,
                    Telefono = config.Telefono,
                    CorreoElectronico = config.CorreoElectronico,
                    SitioWeb = config.SitioWeb,
                    LogoUrl = config.LogoUrl,
                    PiePagina = config.PiePagina,
                    Moneda = config.Moneda,
                    MonedaSecundaria = config.MonedaSecundaria,
                    TipoCambio = tipoCambioActual,
                    Igv = config.Igv
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la configuración de empresa");
                return null;
            }
        }

        public async Task<(bool Success, string Message)> UpdateConfiguracionAsync(EmpresaDto dto)
        {
            try
            {
                var config = await _context.ConfiguracionEmpresas
                    .OrderBy(x => x.IdConfiguracion)
                    .FirstOrDefaultAsync();

                if (config == null)
                {
                    config = new Models.ConfiguracionEmpresa
                    {
                        NombreEmpresa = dto.NombreEmpresa.Trim(),
                        Ruc = dto.Ruc.Trim(),
                        Direccion = LimpiarTexto(dto.Direccion),
                        Telefono = LimpiarTexto(dto.Telefono),
                        CorreoElectronico = LimpiarTexto(dto.CorreoElectronico),
                        SitioWeb = LimpiarTexto(dto.SitioWeb),
                        PiePagina = LimpiarTexto(dto.PiePagina),
                        Moneda = NormalizarMoneda(dto.Moneda, "PEN"),
                        MonedaSecundaria = NormalizarMonedaOpcional(dto.MonedaSecundaria),
                        Igv = dto.Igv,
                        LogoUrl = null
                    };

                    _context.ConfiguracionEmpresas.Add(config);
                    await _context.SaveChangesAsync();

                    await _auditoriaService.RegistrarAsync(
                        null,
                        "Configuracion.ConfiguracionEmpresa",
                        "INSERT",
                        config.IdConfiguracion.ToString(),
                        null,
                        JsonSerializer.Serialize(new
                        {
                            config.NombreEmpresa,
                            config.Ruc,
                            config.Direccion,
                            config.Telefono,
                            config.CorreoElectronico,
                            config.SitioWeb,
                            config.Moneda,
                            config.MonedaSecundaria,
                            config.Igv
                        }));

                    _logger.LogInformation(
                        "Configuración de empresa creada. IdConfiguracion: {IdConfiguracion}",
                        config.IdConfiguracion);

                    return (true, "Configuración guardada correctamente");
                }

                var valoresAnteriores = JsonSerializer.Serialize(new
                {
                    config.NombreEmpresa,
                    config.Ruc,
                    config.Direccion,
                    config.Telefono,
                    config.CorreoElectronico,
                    config.SitioWeb,
                    config.PiePagina,
                    config.Moneda,
                    config.MonedaSecundaria,
                    config.Igv
                });

                config.NombreEmpresa = dto.NombreEmpresa.Trim();
                config.Ruc = dto.Ruc.Trim();
                config.Direccion = LimpiarTexto(dto.Direccion);
                config.Telefono = LimpiarTexto(dto.Telefono);
                config.CorreoElectronico = LimpiarTexto(dto.CorreoElectronico);
                config.SitioWeb = LimpiarTexto(dto.SitioWeb);
                config.PiePagina = LimpiarTexto(dto.PiePagina);
                config.Moneda = NormalizarMoneda(dto.Moneda, "PEN");
                config.MonedaSecundaria = NormalizarMonedaOpcional(dto.MonedaSecundaria);
                config.Igv = dto.Igv;

                /*
                 * IMPORTANTE:
                 * No modificar aquí:
                 *
                 * config.LogoUrl
                 *
                 * LogoUrl se administra mediante UploadLogoAsync/DeleteLogoAsync.
                 * El tipo de cambio operativo se administra mediante TipoCambioService
                 * y la tabla Configuracion.TiposCambio.
                 */

                await _context.SaveChangesAsync();

                var valoresNuevos = JsonSerializer.Serialize(new
                {
                    config.NombreEmpresa,
                    config.Ruc,
                    config.Direccion,
                    config.Telefono,
                    config.CorreoElectronico,
                    config.SitioWeb,
                    config.PiePagina,
                    config.Moneda,
                    config.MonedaSecundaria,
                    config.Igv
                });

                await _auditoriaService.RegistrarAsync(
                    null,
                    "Configuracion.ConfiguracionEmpresa",
                    "UPDATE",
                    config.IdConfiguracion.ToString(),
                    valoresAnteriores,
                    valoresNuevos);

                _logger.LogInformation(
                    "Configuración de empresa actualizada. IdConfiguracion: {IdConfiguracion}",
                    config.IdConfiguracion);

                return (true, "Configuración actualizada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la configuración de empresa");
                return (false, "Error al actualizar la configuración");
            }
        }

        // ============================================================
        // LOGO
        // ============================================================

        public async Task<(bool Success, string Message, string? LogoUrl)> UploadLogoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "No se seleccionó ningún archivo", null);

            try
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                var extensionesPermitidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif",
                    ".svg",
                    ".webp"
                };

                if (!extensionesPermitidas.Contains(extension))
                {
                    return (
                        false,
                        "Formato de archivo no permitido. Use JPG, JPEG, PNG, GIF, SVG o WEBP",
                        null
                    );
                }

                const long tamanioMaximo = 2 * 1024 * 1024;

                if (file.Length > tamanioMaximo)
                    return (false, "El archivo es demasiado grande. Máximo 2 MB", null);

                var config = await _context.ConfiguracionEmpresas
                    .OrderBy(x => x.IdConfiguracion)
                    .FirstOrDefaultAsync();

                if (config == null)
                {
                    return (
                        false,
                        "Primero debe guardar la configuración de la empresa",
                        null
                    );
                }

                if (string.IsNullOrWhiteSpace(_env.WebRootPath))
                {
                    _logger.LogError("WebRootPath no está configurado");
                    return (false, "No se pudo determinar la carpeta de archivos del sistema", null);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "logo");
                Directory.CreateDirectory(uploadsFolder);

                var nombreArchivo = $"logo_{Guid.NewGuid():N}{extension}";
                var rutaCompleta = Path.Combine(uploadsFolder, nombreArchivo);

                await using (var stream = new FileStream(
                    rutaCompleta,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                var nuevaLogoUrl = $"/images/logo/{nombreArchivo}";
                var logoAnterior = config.LogoUrl;

                try
                {
                    config.LogoUrl = nuevaLogoUrl;
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    if (File.Exists(rutaCompleta))
                        File.Delete(rutaCompleta);

                    throw;
                }

                if (!string.IsNullOrWhiteSpace(logoAnterior))
                {
                    try
                    {
                        var rutaAnterior = ObtenerRutaFisicaLogo(logoAnterior);

                        if (rutaAnterior != null && File.Exists(rutaAnterior))
                            File.Delete(rutaAnterior);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "El nuevo logo se guardó, pero no se pudo eliminar el logo anterior: {LogoAnterior}",
                            logoAnterior);
                    }
                }

                await _auditoriaService.RegistrarAsync(
                    null,
                    "Configuracion.ConfiguracionEmpresa",
                    "UPDATE",
                    config.IdConfiguracion.ToString(),
                    JsonSerializer.Serialize(new
                    {
                        LogoUrl = logoAnterior
                    }),
                    JsonSerializer.Serialize(new
                    {
                        LogoUrl = nuevaLogoUrl
                    }));

                _logger.LogInformation(
                    "Logo de empresa actualizado: {LogoUrl}",
                    nuevaLogoUrl);

                return (true, "Logo actualizado correctamente", nuevaLogoUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al subir el logo de empresa");
                return (false, "Error al subir el logo", null);
            }
        }

        public async Task<bool> DeleteLogoAsync()
        {
            try
            {
                var config = await _context.ConfiguracionEmpresas
                    .OrderBy(x => x.IdConfiguracion)
                    .FirstOrDefaultAsync();

                if (config == null)
                    return false;

                if (string.IsNullOrWhiteSpace(config.LogoUrl))
                    return true;

                var logoAnterior = config.LogoUrl;

                config.LogoUrl = null;
                await _context.SaveChangesAsync();

                try
                {
                    var rutaFisica = ObtenerRutaFisicaLogo(logoAnterior);

                    if (rutaFisica != null && File.Exists(rutaFisica))
                        File.Delete(rutaFisica);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Se limpió LogoUrl en BD, pero no se pudo eliminar el archivo físico: {LogoUrl}",
                        logoAnterior);
                }

                await _auditoriaService.RegistrarAsync(
                    null,
                    "Configuracion.ConfiguracionEmpresa",
                    "UPDATE",
                    config.IdConfiguracion.ToString(),
                    JsonSerializer.Serialize(new
                    {
                        LogoUrl = logoAnterior
                    }),
                    JsonSerializer.Serialize(new
                    {
                        LogoUrl = (string?)null
                    }));

                _logger.LogInformation(
                    "Logo de empresa eliminado. IdConfiguracion: {IdConfiguracion}",
                    config.IdConfiguracion);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el logo de empresa");
                return false;
            }
        }

        // ============================================================
        // TIPO DE CAMBIO
        // ============================================================
        //
        // Estos métodos se mantienen porque TiposCambioController
        // actualmente utiliza IEmpresaService para consultar el historial.
        //
        // La actualización automática diaria y la consulta de la API
        // pertenecen a TipoCambioService.
        // ============================================================

        public async Task<decimal> GetTipoCambioActualAsync()
        {
            try
            {
                var tipoCambio = await _context.TiposCambios
                    .AsNoTracking()
                    .Where(t =>
                        t.MonedaOrigen == "USD" &&
                        t.MonedaDestino == "PEN")
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.FechaRegistro)
                    .FirstOrDefaultAsync();

                if (tipoCambio != null)
                    return tipoCambio.Valor;

                return 3.80m;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo obtener el último tipo de cambio desde la base de datos");

                return 3.80m;
            }
        }

        public async Task<TipoCambioDto?> GetUltimoTipoCambioAsync()
        {
            try
            {
                var tipoCambio = await _context.TiposCambios
                    .AsNoTracking()
                    .Include(t => t.Usuario)
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.FechaRegistro)
                    .FirstOrDefaultAsync();

                if (tipoCambio == null)
                    return null;

                return new TipoCambioDto
                {
                    IdTipoCambio = tipoCambio.IdTipoCambio,
                    Fecha = tipoCambio.Fecha,
                    MonedaOrigen = tipoCambio.MonedaOrigen,
                    MonedaDestino = tipoCambio.MonedaDestino,
                    Valor = tipoCambio.Valor,
                    UsuarioId = tipoCambio.UsuarioId,
                    UsuarioNombre = tipoCambio.Usuario != null
                        ? tipoCambio.Usuario.NombreCompleto
                        : "Sistema",
                    FechaRegistro = tipoCambio.FechaRegistro
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el último tipo de cambio");
                return null;
            }
        }

        public async Task<IEnumerable<TipoCambioDto>> GetTiposCambioAsync(
            DateTime? desde = null,
            DateTime? hasta = null)
        {
            try
            {
                var query = _context.TiposCambios
                    .AsNoTracking()
                    .Include(t => t.Usuario)
                    .AsQueryable();

                if (desde.HasValue)
                {
                    var fechaDesde = DateOnly.FromDateTime(desde.Value.Date);
                    query = query.Where(t => t.Fecha >= fechaDesde);
                }

                if (hasta.HasValue)
                {
                    var fechaHasta = DateOnly.FromDateTime(hasta.Value.Date);
                    query = query.Where(t => t.Fecha <= fechaHasta);
                }

                return await query
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.FechaRegistro)
                    .Select(t => new TipoCambioDto
                    {
                        IdTipoCambio = t.IdTipoCambio,
                        Fecha = t.Fecha,
                        MonedaOrigen = t.MonedaOrigen,
                        MonedaDestino = t.MonedaDestino,
                        Valor = t.Valor,
                        UsuarioId = t.UsuarioId,
                        UsuarioNombre = t.Usuario != null
                            ? t.Usuario.NombreCompleto
                            : "Sistema",
                        FechaRegistro = t.FechaRegistro
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el historial de tipos de cambio");
                return new List<TipoCambioDto>();
            }
        }

        public async Task<(bool Success, string Message)> RegistrarTipoCambioAsync(
            TipoCambioDto dto,
            int usuarioId)
        {
            try
            {
                var monedaOrigen = NormalizarMoneda(dto.MonedaOrigen, "USD");
                var monedaDestino = NormalizarMoneda(dto.MonedaDestino, "PEN");

                if (dto.Valor <= 0)
                    return (false, "El tipo de cambio debe ser mayor que cero");

                var existe = await _context.TiposCambios.AnyAsync(t =>
                    t.Fecha == dto.Fecha &&
                    t.MonedaOrigen == monedaOrigen &&
                    t.MonedaDestino == monedaDestino);

                if (existe)
                {
                    return (
                        false,
                        "Ya existe un tipo de cambio para esa fecha y par de monedas"
                    );
                }

                var tipoCambio = new Models.TiposCambio
                {
                    Fecha = dto.Fecha,
                    MonedaOrigen = monedaOrigen,
                    MonedaDestino = monedaDestino,
                    Valor = dto.Valor,
                    UsuarioId = usuarioId > 0 ? usuarioId : null,
                    FechaRegistro = DateTime.Now
                };

                _context.TiposCambios.Add(tipoCambio);
                await _context.SaveChangesAsync();

                await _auditoriaService.RegistrarAsync(
                    usuarioId > 0 ? usuarioId : null,
                    "Configuracion.TiposCambio",
                    "INSERT",
                    tipoCambio.IdTipoCambio.ToString(),
                    null,
                    JsonSerializer.Serialize(new
                    {
                        tipoCambio.Fecha,
                        tipoCambio.MonedaOrigen,
                        tipoCambio.MonedaDestino,
                        tipoCambio.Valor
                    }));

                _logger.LogInformation(
                    "Tipo de cambio registrado: {MonedaOrigen}/{MonedaDestino} = {Valor}",
                    tipoCambio.MonedaOrigen,
                    tipoCambio.MonedaDestino,
                    tipoCambio.Valor);

                return (true, "Tipo de cambio registrado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar el tipo de cambio");
                return (false, "Error al registrar el tipo de cambio");
            }
        }

        public async Task<bool> ExisteTipoCambioAsync(
            DateOnly fecha,
            string monedaOrigen,
            string monedaDestino)
        {
            try
            {
                monedaOrigen = NormalizarMoneda(monedaOrigen, "USD");
                monedaDestino = NormalizarMoneda(monedaDestino, "PEN");

                return await _context.TiposCambios
                    .AsNoTracking()
                    .AnyAsync(t =>
                        t.Fecha == fecha &&
                        t.MonedaOrigen == monedaOrigen &&
                        t.MonedaDestino == monedaDestino);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar existencia de tipo de cambio");
                return false;
            }
        }

        // ============================================================
        // MÉTODOS PRIVADOS
        // ============================================================

        private string? ObtenerRutaFisicaLogo(string? logoUrl)
        {
            if (string.IsNullOrWhiteSpace(logoUrl) ||
                string.IsNullOrWhiteSpace(_env.WebRootPath))
            {
                return null;
            }

            var rutaRelativa = logoUrl
                .Trim()
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var webRoot = Path.GetFullPath(_env.WebRootPath);
            var rutaCompleta = Path.GetFullPath(Path.Combine(webRoot, rutaRelativa));

            if (!rutaCompleta.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            return rutaCompleta;
        }

        private static string? LimpiarTexto(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }

        private static string NormalizarMoneda(string? moneda, string valorPorDefecto)
        {
            if (string.IsNullOrWhiteSpace(moneda))
                return valorPorDefecto;

            return moneda.Trim().ToUpperInvariant();
        }

        private static string? NormalizarMonedaOpcional(string? moneda)
        {
            if (string.IsNullOrWhiteSpace(moneda))
                return null;

            return moneda.Trim().ToUpperInvariant();
        }
    }
}
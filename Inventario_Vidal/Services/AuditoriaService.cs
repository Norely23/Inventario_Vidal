using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Services
{
    public class AuditoriaService : IAuditoriaService
    {
        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly ILogger<AuditoriaService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaService(
            FabricacionesVidalEirlBdContext context,
            ILogger<AuditoriaService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegistrarAsync(
            int? usuarioId,
            string nombreTabla,
            string accion,
            string? idRegistroAfectado,
            string? valoresAnteriores = null,
            string? valoresNuevos = null,
            string? ip = null,
            string? userAgent = null)
        {
            try
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                if (string.IsNullOrEmpty(ip) && httpContext != null)
                {
                    ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
                }
                if (string.IsNullOrEmpty(userAgent) && httpContext != null)
                {
                    userAgent = httpContext.Request.Headers["User-Agent"].ToString() ?? "Unknown";
                }

                var auditoria = new Models.Auditoria
                {
                    IdUsuario = usuarioId,
                    NombreTabla = nombreTabla,
                    Accion = accion,
                    IdRegistroAfectado = idRegistroAfectado,
                    ValoresAnteriores = valoresAnteriores,
                    ValoresNuevos = valoresNuevos,
                    FechaHora = DateTime.Now
                };

                _context.Auditoria.Add(auditoria);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría: {Tabla} - {Accion}", nombreTabla, accion);
            }
        }

        public async Task<List<AuditoriaDto>> GetAuditoriaAsync(
            string? tabla = null,
            string? accion = null,
            int? usuarioId = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            int page = 1,
            int pageSize = 50)
        {
            try
            {
                var query = _context.Auditoria
                    .Include(a => a.IdUsuarioNavigation)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(tabla))
                    query = query.Where(a => a.NombreTabla == tabla);

                if (!string.IsNullOrEmpty(accion))
                    query = query.Where(a => a.Accion == accion);

                if (usuarioId.HasValue)
                    query = query.Where(a => a.IdUsuario == usuarioId);

                if (desde.HasValue)
                    query = query.Where(a => a.FechaHora >= desde.Value);

                if (hasta.HasValue)
                    query = query.Where(a => a.FechaHora <= hasta.Value);

                var skip = (page - 1) * pageSize;

                return await query
                    .OrderByDescending(a => a.FechaHora)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(a => new AuditoriaDto
                    {
                        IdAuditoria = a.IdAuditoria,
                        IdUsuario = a.IdUsuario,
                        UsuarioNombre = a.IdUsuarioNavigation != null ? a.IdUsuarioNavigation.NombreCompleto : "Sistema",
                        NombreTabla = a.NombreTabla,
                        Accion = a.Accion,
                        IdRegistroAfectado = a.IdRegistroAfectado,
                        ValoresAnteriores = a.ValoresAnteriores,
                        ValoresNuevos = a.ValoresNuevos,
                        FechaHora = a.FechaHora,
                        FechaFormateada = a.FechaHora.ToString("dd/MM/yyyy HH:mm:ss")
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAuditoriaAsync");
                return new List<AuditoriaDto>();
            }
        }

        public async Task<List<AuditoriaDto>> GetHistorialRegistroAsync(
            string tabla,
            string idRegistro,
            int? usuarioId = null,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                var query = _context.Auditoria
                    .Include(a => a.IdUsuarioNavigation)
                    .Where(a => a.NombreTabla == tabla && a.IdRegistroAfectado == idRegistro);

                if (usuarioId.HasValue)
                    query = query.Where(a => a.IdUsuario == usuarioId);

                var skip = (page - 1) * pageSize;

                return await query
                    .OrderByDescending(a => a.FechaHora)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(a => new AuditoriaDto
                    {
                        IdAuditoria = a.IdAuditoria,
                        IdUsuario = a.IdUsuario,
                        UsuarioNombre = a.IdUsuarioNavigation != null ? a.IdUsuarioNavigation.NombreCompleto : "Sistema",
                        NombreTabla = a.NombreTabla,
                        Accion = a.Accion,
                        IdRegistroAfectado = a.IdRegistroAfectado,
                        ValoresAnteriores = a.ValoresAnteriores,
                        ValoresNuevos = a.ValoresNuevos,
                        FechaHora = a.FechaHora,
                        FechaFormateada = a.FechaHora.ToString("dd/MM/yyyy HH:mm:ss")
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetHistorialRegistroAsync para {Tabla}/{IdRegistro}", tabla, idRegistro);
                return new List<AuditoriaDto>();
            }
        }

        public async Task<List<string>> GetTablasAuditadasAsync()
        {
            try
            {
                return await _context.Auditoria
                    .Select(a => a.NombreTabla)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetTablasAuditadasAsync");
                return new List<string>();
            }
        }

        public async Task<List<string>> GetAccionesAuditadasAsync()
        {
            try
            {
                return await _context.Auditoria
                    .Select(a => a.Accion)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetAccionesAuditadasAsync");
                return new List<string>();
            }
        }

        public async Task<AuditoriaResumenDto> GetResumenAsync()
        {
            try
            {
                var hoy = DateTime.Today;
                var inicioSemana = hoy.AddDays(-(int)hoy.DayOfWeek + 1);
                var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

                var total = await _context.Auditoria.CountAsync();

                var porTabla = await _context.Auditoria
                    .GroupBy(a => a.NombreTabla)
                    .Select(g => new { Key = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count);

                var porAccion = await _context.Auditoria
                    .GroupBy(a => a.Accion)
                    .Select(g => new { Key = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count);

                var porUsuario = await _context.Auditoria
                    .Where(a => a.IdUsuario != null)
                    .GroupBy(a => a.IdUsuarioNavigation != null ? a.IdUsuarioNavigation.NombreCompleto : "Sistema")
                    .Select(g => new { Key = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Key, x => x.Count);

                var hoyCount = await _context.Auditoria
                    .CountAsync(a => a.FechaHora >= hoy);

                var semanaCount = await _context.Auditoria
                    .CountAsync(a => a.FechaHora >= inicioSemana);

                var mesCount = await _context.Auditoria
                    .CountAsync(a => a.FechaHora >= inicioMes);

                var ultimo = await _context.Auditoria
                    .OrderByDescending(a => a.FechaHora)
                    .Select(a => (DateTime?)a.FechaHora)
                    .FirstOrDefaultAsync();

                return new AuditoriaResumenDto
                {
                    TotalRegistros = total,
                    RegistrosPorTabla = porTabla,
                    RegistrosPorAccion = porAccion,
                    RegistrosPorUsuario = porUsuario,
                    RegistrosHoy = hoyCount,
                    RegistrosSemana = semanaCount,
                    RegistrosMes = mesCount,
                    UltimoRegistro = ultimo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetResumenAsync");
                return new AuditoriaResumenDto();
            }
        }

        public async Task<Dictionary<int, string>> GetUsuariosConActividadAsync()
        {
            try
            {
                return await _context.Auditoria
                    .Where(a => a.IdUsuario != null)
                    .GroupBy(a => a.IdUsuario.Value)
                    .Select(g => new { Key = g.Key, Usuario = g.First().IdUsuarioNavigation.NombreCompleto })
                    .ToDictionaryAsync(x => x.Key, x => x.Usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetUsuariosConActividadAsync");
                return new Dictionary<int, string>();
            }
        }

        // ================================================================
        // MÉTODOS ADICIONALES PARA COMPLETAR LA INTERFAZ
        // ================================================================

        public async Task<Dictionary<DateTime, int>> GetRegistrosPorDiaAsync(int dias = 7)
        {
            try
            {
                var fechaInicio = DateTime.Now.AddDays(-dias);
                var registros = await _context.Auditoria
                    .Where(a => a.FechaHora >= fechaInicio)
                    .GroupBy(a => a.FechaHora.Date)
                    .Select(g => new { Fecha = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Fecha, x => x.Count);

                // Completar días faltantes con 0
                for (int i = 0; i < dias; i++)
                {
                    var fecha = fechaInicio.AddDays(i).Date;
                    if (!registros.ContainsKey(fecha))
                    {
                        registros[fecha] = 0;
                    }
                }

                return registros.OrderBy(r => r.Key).ToDictionary(r => r.Key, r => r.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetRegistrosPorDiaAsync");
                return new Dictionary<DateTime, int>();
            }
        }

        public async Task<Dictionary<string, int>> GetTablasMasAuditadasAsync(int cantidad = 10)
        {
            try
            {
                return await _context.Auditoria
                    .GroupBy(a => a.NombreTabla)
                    .Select(g => new { Tabla = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(cantidad)
                    .ToDictionaryAsync(x => x.Tabla, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetTablasMasAuditadasAsync");
                return new Dictionary<string, int>();
            }
        }
    }
}
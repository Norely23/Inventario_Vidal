using System.Security.Cryptography;
using System.Text;
using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Helpers;
using Inventario_Vidal.Interfaces;
using Inventario_Vidal.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Services
{
    public class AuthService : IAuthService
    {
        private const int MaxIntentosFallidos = 5;
        private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan DuracionSesionNormal = TimeSpan.FromHours(24);
        private static readonly TimeSpan DuracionSesionPersistente = TimeSpan.FromDays(30);

        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            FabricacionesVidalEirlBdContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<AuthResultDto> LoginAsync(LoginRequestDto request)
        {
            var ahora = DateTime.Now;
            var username = (request.Username ?? string.Empty).Trim();
            var ip = Limitar(request.Ip ?? ObtenerIp(), 50);
            var userAgent = Limitar(request.UserAgent ?? ObtenerUserAgent(), 300);

            try
            {
                var usuario = await _context.Usuarios
                    .Include(x => x.IdRolNavigation)
                    .FirstOrDefaultAsync(x => x.NombreUsuario == username);

                if (usuario == null)
                {
                    AgregarBitacora(null, username, "LOGIN_FALLIDO", false, ip, userAgent);
                    await _context.SaveChangesAsync();
                    return AuthResultDto.Error("Usuario o contraseña incorrectos");
                }

                if (!usuario.Activo)
                {
                    AgregarBitacora(usuario.IdUsuario, usuario.NombreUsuario, "LOGIN_INACTIVO", false, ip, userAgent);
                    await _context.SaveChangesAsync();
                    return AuthResultDto.Error("Usuario o contraseña incorrectos");
                }

                if (usuario.IdRolNavigation == null || !usuario.IdRolNavigation.Activo)
                {
                    AgregarBitacora(usuario.IdUsuario, usuario.NombreUsuario, "LOGIN_ROL_INACTIVO", false, ip, userAgent);
                    await _context.SaveChangesAsync();
                    return AuthResultDto.Error("No es posible iniciar sesión con esta cuenta");
                }

                if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value <= ahora)
                {
                    usuario.BloqueadoHasta = null;
                    usuario.IntentosFallidos = 0;
                }

                if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > ahora)
                {
                    AgregarBitacora(usuario.IdUsuario, usuario.NombreUsuario, "LOGIN_BLOQUEADO", false, ip, userAgent);
                    await _context.SaveChangesAsync();
                    return AuthResultDto.Error("La cuenta está temporalmente bloqueada. Intenta nuevamente más tarde.");
                }

                if (!PasswordHelper.VerifyPassword(request.Password, usuario.ContrasenaHash))
                {
                    usuario.IntentosFallidos++;
                    var accion = "LOGIN_FALLIDO";

                    if (usuario.IntentosFallidos >= MaxIntentosFallidos)
                    {
                        usuario.BloqueadoHasta = ahora.Add(DuracionBloqueo);
                        accion = "LOGIN_BLOQUEADO";
                    }

                    AgregarBitacora(usuario.IdUsuario, usuario.NombreUsuario, accion, false, ip, userAgent);
                    await _context.SaveChangesAsync();

                    return usuario.BloqueadoHasta.HasValue
                        ? AuthResultDto.Error("La cuenta fue bloqueada temporalmente por varios intentos fallidos.")
                        : AuthResultDto.Error("Usuario o contraseña incorrectos");
                }

                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
                usuario.UltimoAcceso = ahora;

                var tokensExpirados = await _context.AuthTokens
                    .Where(x => x.UsuarioId == usuario.IdUsuario && x.Activo && x.FechaExpiracion <= ahora)
                    .ToListAsync();

                foreach (var tokenExpirado in tokensExpirados)
                    tokenExpirado.Activo = false;

                var token = GenerarToken();
                var fechaExpiracion = request.RecordarSesion
                    ? ahora.Add(DuracionSesionPersistente)
                    : ahora.Add(DuracionSesionNormal);

                _context.AuthTokens.Add(new AuthTokens
                {
                    UsuarioId = usuario.IdUsuario,
                    TokenHash = HashToken(token),
                    FechaCreacion = ahora,
                    FechaExpiracion = fechaExpiracion,
                    Activo = true,
                    Ip = ip,
                    UserAgent = userAgent,
                    Dispositivo = ObtenerTipoDispositivo(userAgent)
                });

                AgregarBitacora(usuario.IdUsuario, usuario.NombreUsuario, "LOGIN_OK", true, ip, userAgent);
                await _context.SaveChangesAsync();

                SesionHelper.IniciarSesion(
                    _httpContextAccessor,
                    usuario.IdUsuario,
                    usuario.NombreUsuario,
                    usuario.IdRolNavigation.NombreRol,
                    usuario.NombreCompleto,
                    token);

                return AuthResultDto.Ok(
                    usuario.IdUsuario,
                    usuario.NombreCompleto,
                    usuario.NombreUsuario,
                    usuario.CorreoElectronico,
                    usuario.IdRol,
                    usuario.IdRolNavigation.NombreRol,
                    token,
                    request.RecordarSesion,
                    usuario.UltimoAcceso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesión para el usuario {Usuario}", username);
                return AuthResultDto.Error("No se pudo completar el inicio de sesión. Intenta nuevamente.");
            }
        }

        public async Task<AuthResultDto> ValidarSesionAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return AuthResultDto.Error("Sesión no válida");

            try
            {
                var ahora = DateTime.Now;
                var tokenHash = HashToken(token);

                var authToken = await _context.AuthTokens
                    .Include(x => x.Usuario)
                    .ThenInclude(x => x.IdRolNavigation)
                    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.Activo);

                if (authToken == null)
                    return AuthResultDto.Error("Sesión no válida");

                if (authToken.FechaExpiracion <= ahora)
                {
                    authToken.Activo = false;
                    await _context.SaveChangesAsync();
                    return AuthResultDto.Error("Sesión expirada");
                }

                if (authToken.Usuario == null ||
                    !authToken.Usuario.Activo ||
                    authToken.Usuario.IdRolNavigation == null ||
                    !authToken.Usuario.IdRolNavigation.Activo)
                {
                    authToken.Activo = false;
                    await _context.SaveChangesAsync();
                    return AuthResultDto.Error("Sesión no válida");
                }

                var esPersistente = EsTokenPersistente(authToken);

                if (esPersistente && authToken.FechaExpiracion < ahora.AddDays(5))
                    authToken.FechaExpiracion = ahora.Add(DuracionSesionPersistente);

                authToken.Usuario.UltimoAcceso = ahora;
                await _context.SaveChangesAsync();

                return AuthResultDto.Ok(
                    authToken.UsuarioId,
                    authToken.Usuario.NombreCompleto,
                    authToken.Usuario.NombreUsuario,
                    authToken.Usuario.CorreoElectronico,
                    authToken.Usuario.IdRol,
                    authToken.Usuario.IdRolNavigation.NombreRol,
                    token,
                    esPersistente,
                    authToken.Usuario.UltimoAcceso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar una sesión");
                return AuthResultDto.Error("No se pudo validar la sesión");
            }
        }

        public async Task<bool> CerrarSesionAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return true;

            try
            {
                var tokenHash = HashToken(token);
                var authToken = await _context.AuthTokens
                    .Include(x => x.Usuario)
                    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.Activo);

                if (authToken == null)
                    return true;

                authToken.Activo = false;
                AgregarBitacora(
                    authToken.UsuarioId,
                    authToken.Usuario?.NombreUsuario,
                    "LOGOUT",
                    true,
                    Limitar(ObtenerIp(), 50),
                    Limitar(ObtenerUserAgent(), 300));

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesión");
                return false;
            }
        }

        public async Task<bool> CerrarTodasSesionesAsync(int usuarioId)
        {
            try
            {
                var tokens = await _context.AuthTokens
                    .Where(x => x.UsuarioId == usuarioId && x.Activo)
                    .ToListAsync();

                foreach (var token in tokens)
                    token.Activo = false;

                var usuario = await _context.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.IdUsuario == usuarioId);
                _logger.LogInformation("Se cerraron todas las sesiones del usuario {UsuarioId}", usuarioId);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar todas las sesiones del usuario {UsuarioId}", usuarioId);
                return false;
            }
        }

        public async Task<ServiceResultDto> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto request)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);

                if (usuario == null)
                    return ServiceResultDto.Error("Usuario no encontrado");

                if (!PasswordHelper.VerifyPassword(request.PasswordActual, usuario.ContrasenaHash))
                    return ServiceResultDto.Error("La contraseña actual es incorrecta");

                if (PasswordHelper.VerifyPassword(request.PasswordNueva, usuario.ContrasenaHash))
                    return ServiceResultDto.Error("La nueva contraseña debe ser diferente a la actual");

                usuario.ContrasenaHash = PasswordHelper.HashPassword(request.PasswordNueva);
                AgregarBitacora(
                    usuario.IdUsuario,
                    usuario.NombreUsuario,
                    "CAMBIO_PASSWORD",
                    true,
                    Limitar(ObtenerIp(), 50),
                    Limitar(ObtenerUserAgent(), 300));
                await _context.SaveChangesAsync();
                return ServiceResultDto.Ok("Contraseña actualizada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña del usuario {UsuarioId}", usuarioId);
                return ServiceResultDto.Error("No se pudo actualizar la contraseña");
            }
        }

        public async Task<PerfilUsuarioDto?> ObtenerPerfilAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(x => x.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.IdUsuario == usuarioId);

            if (usuario == null)
                return null;

            return new PerfilUsuarioDto
            {
                IdUsuario = usuario.IdUsuario,
                NombreCompleto = usuario.NombreCompleto,
                NombreUsuario = usuario.NombreUsuario,
                CorreoElectronico = usuario.CorreoElectronico,
                NombreRol = usuario.IdRolNavigation?.NombreRol ?? "Sin rol",
                FechaCreacion = usuario.FechaCreacion,
                UltimoAcceso = usuario.UltimoAcceso
            };
        }

        public async Task<ServiceResultDto> ActualizarPerfilAsync(int usuarioId, PerfilUsuarioDto request)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(usuarioId);

                if (usuario == null)
                    return ServiceResultDto.Error("Usuario no encontrado");

                usuario.NombreCompleto = request.NombreCompleto.Trim();
                usuario.CorreoElectronico = string.IsNullOrWhiteSpace(request.CorreoElectronico)
                    ? null
                    : request.CorreoElectronico.Trim();
                AgregarBitacora(
                    usuario.IdUsuario,
                    usuario.NombreUsuario,
                    "PERFIL_ACTUALIZADO",
                    true,
                    Limitar(ObtenerIp(), 50),
                    Limitar(ObtenerUserAgent(), 300));
                await _context.SaveChangesAsync();

                _httpContextAccessor.HttpContext?.Session.SetString("NombreCompleto", usuario.NombreCompleto);
                return ServiceResultDto.Ok("Perfil actualizado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar perfil del usuario {UsuarioId}", usuarioId);
                return ServiceResultDto.Error("No se pudo actualizar el perfil");
            }
        }

        public async Task<List<SesionUsuarioDto>> ObtenerSesionesAsync(int usuarioId, string? tokenActual)
        {
            var ahora = DateTime.Now;
            var tokenHashActual = string.IsNullOrWhiteSpace(tokenActual) ? null : HashToken(tokenActual);

            var expirados = await _context.AuthTokens
                .Where(x => x.UsuarioId == usuarioId && x.Activo && x.FechaExpiracion <= ahora)
                .ToListAsync();

            if (expirados.Count > 0)
            {
                foreach (var token in expirados)
                    token.Activo = false;

                await _context.SaveChangesAsync();
            }

            var tokens = await _context.AuthTokens
                .AsNoTracking()
                .Where(x => x.UsuarioId == usuarioId)
                .OrderByDescending(x => x.FechaCreacion)
                .ToListAsync();

            return tokens.Select(x => new SesionUsuarioDto
            {
                TokenId = x.TokenId,
                FechaCreacion = x.FechaCreacion,
                FechaExpiracion = x.FechaExpiracion,
                Activa = x.Activo && x.FechaExpiracion > ahora,
                EsSesionActual = tokenHashActual != null &&
                                 x.TokenHash != null &&
                                 CryptographicOperations.FixedTimeEquals(x.TokenHash, tokenHashActual),
                Ip = x.Ip,
                UserAgent = x.UserAgent,
                DispositivoRegistrado = x.Dispositivo
            }).ToList();
        }

        public async Task<ServiceResultDto> CerrarSesionRemotaAsync(int usuarioId, long tokenId, string? tokenActual)
        {
            try
            {
                var token = await _context.AuthTokens
                    .Include(x => x.Usuario)
                    .FirstOrDefaultAsync(x =>
                        x.TokenId == tokenId &&
                        x.UsuarioId == usuarioId &&
                        x.Activo);

                if (token == null)
                    return ServiceResultDto.Error("Sesión no encontrada");

                if (EsTokenActual(token, tokenActual))
                    return ServiceResultDto.Error("No puedes cerrar tu sesión actual desde aquí");

                token.Activo = false;
                _logger.LogInformation("Sesión remota {TokenId} cerrada para el usuario {UsuarioId}", tokenId, usuarioId);
                await _context.SaveChangesAsync();
                return ServiceResultDto.Ok("Sesión cerrada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesión remota {TokenId}", tokenId);
                return ServiceResultDto.Error("No se pudo cerrar la sesión");
            }
        }

        public async Task<ServiceResultDto> CerrarSesionesRemotasAsync(int usuarioId, string? tokenActual)
        {
            try
            {
                var tokens = await _context.AuthTokens
                    .Include(x => x.Usuario)
                    .Where(x => x.UsuarioId == usuarioId && x.Activo)
                    .ToListAsync();

                var cerradas = 0;

                foreach (var token in tokens)
                {
                    if (EsTokenActual(token, tokenActual))
                        continue;

                    token.Activo = false;
                    cerradas++;
                }
                _logger.LogInformation("Se cerraron {Cantidad} sesiones remotas del usuario {UsuarioId}", cerradas, usuarioId);
                await _context.SaveChangesAsync();

                return ServiceResultDto.Ok(
                    cerradas == 0
                        ? "No había sesiones remotas activas"
                        : $"Se cerraron {cerradas} sesión(es) remota(s)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar sesiones remotas del usuario {UsuarioId}", usuarioId);
                return ServiceResultDto.Error("No se pudieron cerrar las sesiones remotas");
            }
        }

        private static byte[] HashToken(string token)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(token));
        }

        private static string GenerarToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        }

        private static bool EsTokenPersistente(AuthTokens token)
        {
            return token.FechaExpiracion - token.FechaCreacion > TimeSpan.FromDays(7);
        }

        private static bool EsTokenActual(AuthTokens token, string? tokenActual)
        {
            if (string.IsNullOrWhiteSpace(tokenActual) || token.TokenHash == null)
                return false;

            var hashActual = HashToken(tokenActual);
            return CryptographicOperations.FixedTimeEquals(token.TokenHash, hashActual);
        }

        private void AgregarBitacora(
            int? usuarioId,
            string? nombreUsuario,
            string accion,
            bool exitoso,
            string? ip,
            string? userAgent)
        {
            _context.BitacoraAccesos.Add(new BitacoraAcceso
            {
                UsuarioId = usuarioId,
                NombreUsuario = Limitar(nombreUsuario, 50),
                Accion = Limitar(accion, 50) ?? "ACCESO",
                Ip = Limitar(ip, 50),
                UserAgent = Limitar(userAgent, 300),
                Exitoso = exitoso,
                FechaHora = DateTime.Now
            });
        }

        private string ObtenerIp()
        {
            var context = _httpContextAccessor.HttpContext;

            if (context == null)
                return "0.0.0.0";

            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
        }

        private string ObtenerUserAgent()
        {
            var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
            return string.IsNullOrWhiteSpace(userAgent) ? "Unknown" : userAgent;
        }

        private static string ObtenerTipoDispositivo(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return "Desconocido";

            if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
                return "Tablet";

            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                return "Móvil";

            return "Escritorio";
        }

        private static string? Limitar(string? valor, int longitudMaxima)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            valor = valor.Trim();
            return valor.Length <= longitudMaxima ? valor : valor[..longitudMaxima];
        }
    }
}

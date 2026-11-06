using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResultDto> LoginAsync(LoginRequestDto request);
        Task<AuthResultDto> ValidarSesionAsync(string token);
        Task<bool> CerrarSesionAsync(string token);
        Task<bool> CerrarTodasSesionesAsync(int usuarioId);
        Task<ServiceResultDto> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto request);
        Task<PerfilUsuarioDto?> ObtenerPerfilAsync(int usuarioId);
        Task<ServiceResultDto> ActualizarPerfilAsync(int usuarioId, PerfilUsuarioDto request);
        Task<List<SesionUsuarioDto>> ObtenerSesionesAsync(int usuarioId, string? tokenActual);
        Task<ServiceResultDto> CerrarSesionRemotaAsync(int usuarioId, long tokenId, string? tokenActual);
        Task<ServiceResultDto> CerrarSesionesRemotasAsync(int usuarioId, string? tokenActual);
    }
}

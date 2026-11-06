using Inventario_Vidal.DTOs;

namespace Inventario_Vidal.Interfaces
{
    public interface IEmpresaService
    {
        /// <summary>
        /// Obtiene la configuración de la empresa
        /// </summary>
        Task<EmpresaDto?> GetConfiguracionAsync();

        /// <summary>
        /// Actualiza la configuración de la empresa
        /// </summary>
        Task<(bool Success, string Message)> UpdateConfiguracionAsync(EmpresaDto dto);

        /// <summary>
        /// Sube el logo de la empresa
        /// </summary>
        Task<(bool Success, string Message, string? LogoUrl)> UploadLogoAsync(IFormFile file);

        /// <summary>
        /// Elimina el logo de la empresa
        /// </summary>
        Task<bool> DeleteLogoAsync();

        /// <summary>
        /// Obtiene el tipo de cambio actual
        /// </summary>
        Task<decimal> GetTipoCambioActualAsync();

        /// <summary>
        /// Obtiene el historial de tipos de cambio
        /// </summary>
        Task<IEnumerable<TipoCambioDto>> GetTiposCambioAsync(DateTime? desde = null, DateTime? hasta = null);

        /// <summary>
        /// Registra un nuevo tipo de cambio
        /// </summary>
        Task<(bool Success, string Message)> RegistrarTipoCambioAsync(TipoCambioDto dto, int usuarioId);

        /// <summary>
        /// Obtiene el último tipo de cambio registrado
        /// </summary>
        Task<TipoCambioDto?> GetUltimoTipoCambioAsync();

        /// <summary>
        /// Verifica si existe un tipo de cambio para una fecha específica
        /// </summary>
        Task<bool> ExisteTipoCambioAsync(DateOnly fecha, string monedaOrigen, string monedaDestino);
    }
}
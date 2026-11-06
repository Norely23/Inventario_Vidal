using InventarioVidal.DTOs;

namespace InventarioVidal.Services.Interfaces
{
    public interface ITipoCambioService
    {
        Task<TipoCambioDTO> ObtenerTipoCambioActualAsync(int? idUsuario = null);

        // Convierte usando el tipo de cambio del día (ya cacheado)
        Task<decimal> ConvertirAsync(decimal monto, string monedaOrigen, string monedaDestino);
    }
}
using System.Text.Json.Serialization;

namespace InventarioVidal.DTOs
{
    // Respuesta de https://open.er-api.com/v6/latest/USD (ExchangeRate-API, Open Access, gratis y sin token)
    // Ejemplo: { "result": "success", "base_code": "USD", "rates": { "PEN": 3.37, "EUR": 0.86, ... } }
    public class TipoCambioApiResponseDTO
    {
        [JsonPropertyName("result")]
        public string Result { get; set; } = string.Empty;

        [JsonPropertyName("base_code")]
        public string BaseCode { get; set; } = string.Empty;

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

    public class TipoCambioDTO
    {
        public int IdTipoCambio { get; set; }
        public decimal Valor { get; set; }
        public decimal Compra { get; set; }
        public decimal Venta { get; set; }
        public DateTime Fecha { get; set; }
        public bool VieneDeCache { get; set; }
        public bool VieneDeFallback { get; set; }
    }
}
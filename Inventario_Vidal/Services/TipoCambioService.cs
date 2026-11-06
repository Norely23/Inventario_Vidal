using Inventario_Vidal.Data;
using Inventario_Vidal.Models;
using InventarioVidal.DTOs;
using InventarioVidal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace InventarioVidal.Services
{
    // ====================================================================
    // Servicio de Tipo de Cambio
    // - Usa el mismo DbContext / tabla (Configuracion.TiposCambio) que el
    //   resto del sistema (EmpresaService), evitando datos duplicados.
    // - Llama a la API GRATUITA "ExchangeRate-API - Open Access"
    //   (https://open.er-api.com) SOLO cuando no existe todavía un
    //   registro del día en la BD. Sin token, sin registro, sin límite de
    //   uso. Se actualiza 1 vez al día en origen. El resto de las veces
    //   que se carga la vista, sirve el valor ya cacheado en BD.
    // ====================================================================
    public class TipoCambioService : ITipoCambioService
    {
        private const string MONEDA_ORIGEN = "USD";
        private const string MONEDA_DESTINO = "PEN";

        private readonly FabricacionesVidalEirlBdContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TipoCambioService> _logger;

        public TipoCambioService(
            FabricacionesVidalEirlBdContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<TipoCambioService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<TipoCambioDTO> ObtenerTipoCambioActualAsync(int? idUsuario = null)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Now);

            // 1. ¿Ya existe el registro de HOY en la BD? -> se usa tal cual (sin llamar a la API)
            var registroHoy = await _context.TiposCambios
                .Where(t => t.Fecha == hoy && t.MonedaOrigen == MONEDA_ORIGEN && t.MonedaDestino == MONEDA_DESTINO)
                .OrderByDescending(t => t.FechaRegistro)
                .FirstOrDefaultAsync();

            if (registroHoy != null)
            {
                return Mapear(registroHoy, vieneDeCache: true, vieneDeFallback: false);
            }

            // 2. No existe registro de hoy -> consultar la API externa (gratuita, sin token)
            try
            {
                var cliente = _httpClientFactory.CreateClient("TipoCambioApi");
                var respuesta = await cliente.GetAsync("v6/latest/USD");

                if (!respuesta.IsSuccessStatusCode)
                    throw new Exception($"La API respondió con estado {respuesta.StatusCode}");

                var json = await respuesta.Content.ReadAsStringAsync();
                var datos = JsonSerializer.Deserialize<TipoCambioApiResponseDTO>(json);

                if (datos == null || !datos.Rates.TryGetValue(MONEDA_DESTINO, out var valorPen) || valorPen <= 0)
                    throw new Exception("La respuesta de la API vino vacía o sin el valor de PEN.");

                // 3. Guardar el registro del día UNA sola vez (vía EF Core, misma tabla que el resto del sistema)
                var nuevo = new TiposCambio
                {
                    Fecha = hoy,
                    MonedaOrigen = MONEDA_ORIGEN,
                    MonedaDestino = MONEDA_DESTINO,
                    Valor = valorPen,
                    UsuarioId = idUsuario,
                    FechaRegistro = DateTime.Now
                };

                _context.TiposCambios.Add(nuevo);
                await _context.SaveChangesAsync();

                return new TipoCambioDTO
                {
                    IdTipoCambio = nuevo.IdTipoCambio,
                    Valor = valorPen,
                    Compra = valorPen,
                    Venta = valorPen,
                    Fecha = DateTime.Now,
                    VieneDeCache = false,
                    VieneDeFallback = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al consultar la API de tipo de cambio. Se usará el último valor guardado en BD.");

                // 4. Fallback: último valor guardado en BD, sin importar la fecha
                var ultimo = await _context.TiposCambios
                    .OrderByDescending(t => t.Fecha)
                    .ThenByDescending(t => t.FechaRegistro)
                    .FirstOrDefaultAsync();

                if (ultimo != null)
                {
                    return Mapear(ultimo, vieneDeCache: true, vieneDeFallback: true);
                }

                // 5. Último recurso: valor por defecto fijo
                _logger.LogError("No hay ningún tipo de cambio guardado en BD. Se usa valor por defecto 3.80.");
                return new TipoCambioDTO
                {
                    Valor = 3.80m,
                    Compra = 3.80m,
                    Venta = 3.80m,
                    Fecha = DateTime.Now,
                    VieneDeCache = true,
                    VieneDeFallback = true
                };
            }
        }

        public async Task<decimal> ConvertirAsync(decimal monto, string monedaOrigen, string monedaDestino)
        {
            monedaOrigen = monedaOrigen.ToUpper();
            monedaDestino = monedaDestino.ToUpper();

            if (monedaOrigen == monedaDestino)
                return monto;

            var tc = await ObtenerTipoCambioActualAsync();

            if (monedaOrigen == "USD" && monedaDestino == "PEN")
                return Math.Round(monto * tc.Valor, 2);

            if (monedaOrigen == "PEN" && monedaDestino == "USD")
                return Math.Round(monto / tc.Valor, 2);

            throw new NotSupportedException($"Conversión de {monedaOrigen} a {monedaDestino} no soportada.");
        }

        private static TipoCambioDTO Mapear(TiposCambio entidad, bool vieneDeCache, bool vieneDeFallback)
        {
            return new TipoCambioDTO
            {
                IdTipoCambio = entidad.IdTipoCambio,
                Fecha = entidad.Fecha.ToDateTime(TimeOnly.MinValue),
                Valor = entidad.Valor,
                Compra = entidad.Valor,
                Venta = entidad.Valor,
                VieneDeCache = vieneDeCache,
                VieneDeFallback = vieneDeFallback
            };
        }
    }
}
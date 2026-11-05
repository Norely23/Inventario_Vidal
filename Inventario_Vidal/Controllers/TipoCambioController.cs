using InventarioVidal.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace InventarioVidal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TipoCambioController : ControllerBase
    {
        private readonly ITipoCambioService _tipoCambioService;
        private readonly ILogger<TipoCambioController> _logger;

        public TipoCambioController(
            ITipoCambioService tipoCambioService,
            ILogger<TipoCambioController> logger)
        {
            _tipoCambioService = tipoCambioService;
            _logger = logger;
        }

        // ============================================================
        // GET: api/TipoCambio/Actual
        // ============================================================

        [HttpGet("Actual")]
        public async Task<IActionResult> Actual()
        {
            try
            {
                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var resultado = await _tipoCambioService.ObtenerTipoCambioActualAsync(idUsuario);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        resultado.IdTipoCambio,
                        resultado.Valor,
                        resultado.Compra,
                        resultado.Venta,
                        fecha = resultado.Fecha.ToString("dd/MM/yyyy"),
                        resultado.VieneDeCache,
                        resultado.VieneDeFallback
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el tipo de cambio actual");

                return StatusCode(500, new
                {
                    success = false,
                    message = "No se pudo obtener el tipo de cambio actual."
                });
            }
        }

        // ============================================================
        // GET: api/TipoCambio/Convertir
        // Ejemplo:
        // api/TipoCambio/Convertir?monto=100&origen=USD&destino=PEN
        // ============================================================

        [HttpGet("Convertir")]
        public async Task<IActionResult> Convertir(
            decimal monto,
            string origen,
            string destino)
        {
            if (string.IsNullOrWhiteSpace(origen) ||
                string.IsNullOrWhiteSpace(destino))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Debe indicar la moneda de origen y destino."
                });
            }

            if (monto < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "El monto no puede ser negativo."
                });
            }

            try
            {
                origen = origen.Trim().ToUpperInvariant();
                destino = destino.Trim().ToUpperInvariant();

                var resultado = await _tipoCambioService.ConvertirAsync(
                    monto,
                    origen,
                    destino);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        montoOriginal = monto,
                        monedaOrigen = origen,
                        monedaDestino = destino,
                        montoConvertido = resultado
                    }
                });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al convertir {Monto} de {Origen} a {Destino}",
                    monto,
                    origen,
                    destino);

                return StatusCode(500, new
                {
                    success = false,
                    message = "No se pudo realizar la conversión."
                });
            }
        }
    }
}
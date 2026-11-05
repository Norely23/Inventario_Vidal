using System;
using System.Threading;
using System.Threading.Tasks;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Inventario_Vidal.Controllers
{
    /// <summary>
    /// Punto de entrada del asistente VIDAL. No modifica inventario ni genera compras.
    /// Exige sesión y el permiso IA.CONSULTAR; cada consulta de datos vuelve a verificar
    /// el permiso del módulo correspondiente en la capa de servicio.
    /// </summary>
    [LoginRequerido]
    [PermisoRequerido("IA.CONSULTAR")]
    public sealed class AsistenteController : Controller
    {
        private const int LimiteCuerpoBytes = 262144;

        private readonly IAsistenteIAService _asistente;
        private readonly ILogger<AsistenteController> _logger;

        public AsistenteController(
            IAsistenteIAService asistente,
            ILogger<AsistenteController> logger)
        {
            _asistente = asistente ?? throw new ArgumentNullException(nameof(asistente));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!TryObtenerUsuarioId(out _))
                return RedirectToAction("Login", "Auth");

            return View();
        }

        /// <summary>
        /// Comprueba Ollama y el modelo configurado.
        /// URL: GET /Asistente/Diagnostico
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Diagnostico(CancellationToken cancellationToken)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
                return Unauthorized(new { mensaje = "La sesión ha expirado. Inicie sesión nuevamente." });

            try
            {
                var resultado = await _asistente.DiagnosticarAsync(usuarioId, cancellationToken);
                return Ok(resultado);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499, new { mensaje = "La solicitud fue cancelada." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al diagnosticar el asistente VIDAL (usuario {UsuarioId})", usuarioId);
                return StatusCode(500, new { mensaje = "No se pudo realizar el diagnóstico." });
            }
        }

        /// <summary>
        /// Recibe un mensaje del chat y devuelve datos del inventario, preguntas pendientes o una propuesta.
        /// URL: POST /Asistente/Consultar
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/json")]
        [RequestSizeLimit(LimiteCuerpoBytes)]
        public async Task<IActionResult> Consultar(
            [FromBody] ConsultaAsistenteSolicitudDto solicitud,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
                return Unauthorized(new { mensaje = "La sesión ha expirado. Inicie sesión nuevamente." });

            if (solicitud is null || !ModelState.IsValid)
                return BadRequest(new { mensaje = "La consulta contiene datos inválidos.", errores = ModelState });

            try
            {
                var resultado = await _asistente.ConsultarAsync(solicitud, usuarioId, cancellationToken);
                return Ok(resultado);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499, new { mensaje = "La solicitud fue cancelada." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "No tienes permiso para esta operación." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar el asistente VIDAL (usuario {UsuarioId})", usuarioId);
                return StatusCode(500, new { mensaje = "No se pudo procesar la consulta del asistente." });
            }
        }

        /// <summary>
        /// Genera una BOM preliminar y calcula faltantes sin reservar stock.
        /// URL: POST /Asistente/PlanificarMateriales
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("application/json")]
        [RequestSizeLimit(LimiteCuerpoBytes)]
        public async Task<IActionResult> PlanificarMateriales(
            [FromBody] PlanificacionMaterialesSolicitudDto solicitud,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
                return Unauthorized(new { mensaje = "La sesión ha expirado. Inicie sesión nuevamente." });

            if (solicitud is null || !ModelState.IsValid)
                return BadRequest(new { mensaje = "Los datos de planificación no son válidos.", errores = ModelState });

            try
            {
                var resultado = await _asistente.PlanificarMaterialesAsync(solicitud, usuarioId, cancellationToken);
                return Ok(resultado);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499, new { mensaje = "La solicitud fue cancelada." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "No tienes permiso para esta operación." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al planificar materiales (usuario {UsuarioId})", usuarioId);
                return StatusCode(500, new { mensaje = "No se pudo generar la propuesta de materiales." });
            }
        }

        /// <summary>
        /// Recupera un borrador temporal perteneciente al usuario autenticado.
        /// URL: GET /Asistente/ObtenerPlanificacion?idPlanificacion=...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerPlanificacion(
            [FromQuery] Guid idPlanificacion,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
                return Unauthorized(new { mensaje = "La sesión ha expirado. Inicie sesión nuevamente." });

            if (idPlanificacion == Guid.Empty)
                return BadRequest(new { mensaje = "El identificador de planificación no es válido." });

            try
            {
                var resultado = await _asistente.ObtenerPlanificacionAsync(idPlanificacion, usuarioId, cancellationToken);
                return resultado is null ? NotFound(new { mensaje = "No se encontró la planificación." }) : Ok(resultado);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499, new { mensaje = "La solicitud fue cancelada." });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "No tienes permiso para esta operación." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planificación {IdPlanificacion}", idPlanificacion);
                return StatusCode(500, new { mensaje = "No se pudo recuperar la planificación." });
            }
        }

        /// <summary>
        /// Registra la decisión humana sobre una propuesta. Requiere el permiso IA.APROBAR
        /// (se crea en el script SQL de la entrega de persistencia). No crea órdenes de compra.
        /// URL: POST /Asistente/RevisarPlanificacion
        /// </summary>
        [HttpPost]
        [PermisoRequerido("IA.APROBAR")]
        [ValidateAntiForgeryToken]
        [Consumes("application/json")]
        [RequestSizeLimit(LimiteCuerpoBytes)]
        public async Task<IActionResult> RevisarPlanificacion(
            [FromBody] RevisionPlanificacionSolicitudDto solicitud,
            CancellationToken cancellationToken)
        {
            if (!TryObtenerUsuarioId(out var usuarioId))
                return Unauthorized(new { mensaje = "La sesión ha expirado. Inicie sesión nuevamente." });

            if (solicitud is null || !ModelState.IsValid || solicitud.IdPlanificacion == Guid.Empty)
                return BadRequest(new { mensaje = "La revisión contiene datos inválidos.", errores = ModelState });

            try
            {
                var (exito, mensaje) = await _asistente.RevisarPlanificacionAsync(solicitud, usuarioId, cancellationToken);
                return exito ? Ok(new { mensaje }) : Conflict(new { mensaje });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(499, new { mensaje = "La solicitud fue cancelada." });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { mensaje = "No tienes permiso para esta operación." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al revisar planificación {IdPlanificacion}", solicitud.IdPlanificacion);
                return StatusCode(500, new { mensaje = "No se pudo procesar la revisión." });
            }
        }

        private bool TryObtenerUsuarioId(out int usuarioId)
        {
            usuarioId = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
            return usuarioId > 0;
        }
    }
}
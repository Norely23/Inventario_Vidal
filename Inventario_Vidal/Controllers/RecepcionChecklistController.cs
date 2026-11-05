using System.Data;
using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers;

[LoginRequerido]
[PermisoRequerido("COMPRA.RECIBIR")]
public class RecepcionChecklistController : Controller
{
    private readonly FabricacionesVidalEirlBdContext _db;
    private readonly ILogger<RecepcionChecklistController> _logger;

    public RecepcionChecklistController(FabricacionesVidalEirlBdContext db, ILogger<RecepcionChecklistController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        if (id <= 0) return BadRequest();
        try
        {
            var modelo = await CargarAsync(id);
            return modelo == null ? NotFound() : View(modelo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar checklist de compra {IdCompra}", id);
            TempData["MensajeError"] = "No se pudo consultar el checklist.";
            return RedirectToAction("Detalles", "Compras", new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iniciar(int id)
    {
        if (id <= 0) return BadRequest();
        var usuarioId = HttpContext.Session.GetInt32("IdUsuario");
        if (!usuarioId.HasValue || usuarioId.Value <= 0) return Unauthorized();
        try
        {
            await using var command = _db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "Compras.sp_RecepcionChecklist_Iniciar";
            command.CommandType = CommandType.StoredProcedure;
            Agregar(command, "@IdCompra", id, DbType.Int32);
            Agregar(command, "@IdUsuario", usuarioId.Value, DbType.Int32);
            var salida = command.CreateParameter();
            salida.ParameterName = "@IdRecepcion";
            salida.DbType = DbType.Int64;
            salida.Direction = ParameterDirection.Output;
            command.Parameters.Add(salida);
            await _db.Database.OpenConnectionAsync();
            try { await command.ExecuteNonQueryAsync(); }
            finally { await _db.Database.CloseConnectionAsync(); }
            TempData["MensajeExito"] = "Checklist de recepción iniciado.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo iniciar checklist de compra {IdCompra}", id);
            TempData["MensajeError"] = "No se pudo iniciar: " + ex.GetBaseException().Message;
        }
        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarConteo(RegistrarConteoDto dto)
    {
        if (dto.IdCompra <= 0 || dto.IdRecepcion <= 0 || dto.IdDetalleCompra <= 0 || dto.CantidadContada < 0) return BadRequest();
        try
        {
            if (!await PerteneceAsync(dto.IdCompra, dto.IdRecepcion)) return BadRequest();
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC Compras.sp_RecepcionChecklist_RegistrarConteo @IdRecepcion={dto.IdRecepcion}, @IdDetalleCompra={dto.IdDetalleCompra}, @CantidadContada={dto.CantidadContada}");
            TempData["MensajeExito"] = "Conteo guardado.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al guardar conteo de recepción {IdRecepcion}", dto.IdRecepcion);
            TempData["MensajeError"] = "No se pudo guardar el conteo: " + ex.GetBaseException().Message;
        }
        return RedirectToAction(nameof(Index), new { id = dto.IdCompra });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(CerrarChecklistDto dto)
    {
        if (dto.IdCompra <= 0 || dto.IdRecepcion <= 0) return BadRequest();
        try
        {
            if (!await PerteneceAsync(dto.IdCompra, dto.IdRecepcion)) return BadRequest();
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC Compras.sp_RecepcionChecklist_Cerrar @IdRecepcion={dto.IdRecepcion}");
            TempData["MensajeExito"] = "Checklist cerrado. Revisa su estado antes de recibir la compra.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al cerrar checklist {IdRecepcion}", dto.IdRecepcion);
            TempData["MensajeError"] = "No se pudo cerrar: " + ex.GetBaseException().Message;
        }
        return RedirectToAction(nameof(Index), new { id = dto.IdCompra });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReabrirDiferencia(long idRecepcion, int idCompra, string motivo)
    {
        if (idCompra <= 0 || idRecepcion <= 0 || string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 10)
            return BadRequest("Indique un motivo válido de al menos 10 caracteres.");
        var usuarioId = HttpContext.Session.GetInt32("IdUsuario");
        if (!usuarioId.HasValue || usuarioId.Value <= 0) return Unauthorized();
        try
        {
            if (!await PerteneceAsync(idCompra, idRecepcion)) return BadRequest();
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC Compras.sp_RecepcionChecklist_ReabrirDiferencia @IdRecepcion={idRecepcion}, @IdUsuario={usuarioId.Value}, @Motivo={motivo.Trim()}");
            TempData["MensajeExito"] = "Checklist reabierto. Corrija las cantidades y vuelva a cerrarlo.";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo reabrir checklist {IdRecepcion}", idRecepcion);
            TempData["MensajeError"] = "No se pudo reabrir el checklist.";
        }
        return RedirectToAction(nameof(Index), new { id = idCompra });
    }

    private async Task<bool> PerteneceAsync(int idCompra, long idRecepcion)
    {
        return await _db.Database.SqlQueryRaw<int>(
            "SELECT CONVERT(INT,COUNT_BIG(*)) AS [Value] FROM Compras.RecepcionesChecklist WHERE IdCompra={0} AND IdRecepcion={1}", idCompra, idRecepcion).SingleAsync() == 1;
    }

    private async Task<RecepcionChecklistDto?> CargarAsync(int idCompra)
    {
        var compra = await _db.Compras.AsNoTracking().Where(x => x.IdCompra == idCompra)
            .Select(x => new { x.IdCompra, x.NumeroComprobante, Proveedor = x.IdProveedorNavigation.RazonSocial }).FirstOrDefaultAsync();
        if (compra == null) return null;
        var result = new RecepcionChecklistDto { IdCompra = idCompra, NumeroComprobante = compra.NumeroComprobante, Proveedor = compra.Proveedor };
        var conexion = _db.Database.GetDbConnection();
        await _db.Database.OpenConnectionAsync();
        try
        {
            await using (var cab = conexion.CreateCommand())
            {
                cab.CommandText = "SELECT IdRecepcion,Estado FROM Compras.RecepcionesChecklist WHERE IdCompra=@compra";
                Agregar(cab, "@compra", idCompra, DbType.Int32);
                await using var lector = await cab.ExecuteReaderAsync();
                if (await lector.ReadAsync())
                {
                    result.IdRecepcion = lector.GetInt64(0);
                    result.Estado = lector.GetString(1);
                }
            }
            await using (var det = conexion.CreateCommand())
            {
                det.CommandText = @"SELECT d.IdDetalleCompra,d.IdMaterial,m.CodigoBarras,m.Nombre,d.Cantidad,d.FactorConversionUsado,
cd.CantidadContada,COALESCE(cm.CantidadManual,0) AS CantidadManual,COALESCE(q.CantidadQr,0) AS CantidadQr,
CASE WHEN cd.IdDetalleCompra IS NULL THEN CAST(0 AS BIT) ELSE CAST(1 AS BIT) END AS TieneConteo
FROM Compras.DetalleCompras d
INNER JOIN Inventario.Materiales m ON m.IdMaterial=d.IdMaterial
LEFT JOIN Compras.RecepcionesChecklistDetalle cd ON cd.IdDetalleCompra=d.IdDetalleCompra AND cd.IdRecepcion=@recepcion
LEFT JOIN Compras.ConteosManualesRecepcion cm ON cm.IdDetalleCompra=d.IdDetalleCompra AND cm.IdRecepcion=@recepcion
OUTER APPLY(
    SELECT SUM(l.Cantidad) AS CantidadQr
    FROM Compras.LecturasQrRecepcion l
    INNER JOIN Compras.SesionesQrRecepcion s ON s.IdSesionQr=l.IdSesionQr
    WHERE s.IdRecepcion=@recepcion AND l.IdDetalleCompra=d.IdDetalleCompra
) q
WHERE d.IdCompra=@compra ORDER BY d.IdDetalleCompra";
                Agregar(det, "@recepcion", result.IdRecepcion, DbType.Int64);
                Agregar(det, "@compra", idCompra, DbType.Int32);
                await using var lector = await det.ExecuteReaderAsync();
                while (await lector.ReadAsync())
                {
                    result.Detalles.Add(new RecepcionChecklistDetalleDto
                    {
                        IdDetalleCompra = lector.GetInt32(0),
                        IdMaterial = lector.GetInt32(1),
                        CodigoBarras = lector.GetString(2),
                        Material = lector.GetString(3),
                        CantidadEsperada = lector.GetDecimal(4),
                        FactorConversionUsado = lector.GetDecimal(5),
                        CantidadContada = lector.IsDBNull(6) ? null : lector.GetDecimal(6),
                        CantidadManual = lector.GetDecimal(7),
                        CantidadQr = lector.GetDecimal(8),
                        TieneConteo = lector.GetBoolean(9)
                    });
                }
            }
        }
        finally { await _db.Database.CloseConnectionAsync(); }
        return result;
    }

    private static void Agregar(System.Data.Common.DbCommand command, string nombre, object valor, DbType tipo)
    {
        var p = command.CreateParameter();
        p.ParameterName = nombre; p.Value = valor; p.DbType = tipo;
        command.Parameters.Add(p);
    }
}

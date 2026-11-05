using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Inventario_Vidal.Controllers;

public class RecepcionQrController : Controller
{
    private readonly FabricacionesVidalEirlBdContext _db;
    private readonly IEscaneoMovilService _escaneo;
    private readonly ILogger<RecepcionQrController> _logger;

    public RecepcionQrController(FabricacionesVidalEirlBdContext db, IEscaneoMovilService escaneo, ILogger<RecepcionQrController> logger)
    {
        _db = db;
        _escaneo = escaneo;
        _logger = logger;
    }

    [HttpPost]
    [LoginRequerido]
    [PermisoRequerido("COMPRA.RECIBIR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vincular(VincularQrRecepcionDto dto)
    {
        var usuario = HttpContext.Session.GetInt32("IdUsuario");
        if (!usuario.HasValue || usuario.Value <= 0) return Unauthorized();
        if (dto.IdCompra <= 0 || string.IsNullOrWhiteSpace(dto.Token)) return BadRequest();
        if (!_escaneo.EsCreadorSesion(dto.Token, usuario.Value)) return Forbid();
        if (_escaneo.ObtenerProceso(dto.Token) != TiposProcesoEscaneo.Recepcion) return BadRequest("El QR no es de recepción.");
        try
        {
            var proveedor = await _db.Compras.AsNoTracking().Where(x => x.IdCompra == dto.IdCompra && x.Estado == "Registrada")
                .Select(x => (int?)x.IdProveedor).SingleOrDefaultAsync();
            if (!proveedor.HasValue || _escaneo.ObtenerIdProveedor(dto.Token) != proveedor.Value)
                return BadRequest("El proveedor del QR no coincide con la compra.");
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC Compras.sp_SesionQrRecepcion_Vincular @Token={dto.Token},@IdCompra={dto.IdCompra},@IdUsuario={usuario.Value}");
            return Ok(new { success = true, message = "Sesión vinculada al checklist." });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo vincular QR a compra {IdCompra}", dto.IdCompra);
            return BadRequest(new { success = false, message = "No se pudo vincular la sesión. Verifica el checklist y que el token no esté vinculado." });
        }
    }

    [HttpPost]
    [PermitirAccesoPublico]
    public async Task<IActionResult> RegistrarLectura([FromBody] LecturaQrRecepcionDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Token) || dto.IdDetalleCompra <= 0 || dto.IdMaterial <= 0 || dto.Cantidad <= 0 || dto.IdentificadorLectura == Guid.Empty)
            return BadRequest(new { success = false, message = "Datos de lectura inválidos." });
        if (!_escaneo.ExisteSesionValida(dto.Token) || _escaneo.ObtenerProceso(dto.Token) != TiposProcesoEscaneo.Recepcion)
            return Unauthorized(new { success = false, message = "La sesión QR expiró." });
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC Compras.sp_SesionQrRecepcion_RegistrarLectura @Token={dto.Token},@IdentificadorLectura={dto.IdentificadorLectura},@IdDetalleCompra={dto.IdDetalleCompra},@IdMaterial={dto.IdMaterial},@IdPresentacion={dto.IdPresentacion},@Cantidad={dto.Cantidad}");
            return Ok(new { success = true, message = "Lectura registrada en el checklist." });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lectura QR rechazada para detalle {IdDetalleCompra}", dto.IdDetalleCompra);
            return BadRequest(new { success = false, message = "La lectura fue rechazada. Verifica línea, presentación, estado o posible duplicado." });
        }
    }
    [HttpGet]
    [PermitirAccesoPublico]
    public async Task<IActionResult> Movil(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !_escaneo.ExisteSesionValida(token) || _escaneo.ObtenerProceso(token) != TiposProcesoEscaneo.Recepcion)
            return Unauthorized("La sesión QR es inválida o ha expirado.");
        var lineas = new List<LineaMovil>();
        var conexion = _db.Database.GetDbConnection();
        var abrir = conexion.State != ConnectionState.Open;
        try
        {
            if (abrir) await conexion.OpenAsync();
            await using var cmd = conexion.CreateCommand();
            cmd.CommandText = @"SELECT d.IdDetalleCompra,d.IdMaterial,d.IdPresentacion,m.CodigoBarras,m.Nombre,COALESCE(pm.Nombre,'Unidad base') AS Presentacion,d.Cantidad,COALESCE(cd.CantidadContada,0) AS Contada
FROM Compras.SesionesQrRecepcion s
JOIN Compras.RecepcionesChecklist r ON r.IdRecepcion=s.IdRecepcion
JOIN Compras.Compras c ON c.IdCompra=r.IdCompra AND c.Estado='Registrada'
JOIN Compras.DetalleCompras d ON d.IdCompra=r.IdCompra
JOIN Inventario.Materiales m ON m.IdMaterial=d.IdMaterial
LEFT JOIN Inventario.PresentacionesMaterial pm ON pm.IdPresentacion=d.IdPresentacion
LEFT JOIN Compras.RecepcionesChecklistDetalle cd ON cd.IdRecepcion=r.IdRecepcion AND cd.IdDetalleCompra=d.IdDetalleCompra
WHERE s.TokenHash=HASHBYTES('SHA2_256',@Token) AND s.Activa=1 AND s.FechaExpiracion>SYSDATETIME() AND r.Estado='Borrador'
ORDER BY d.IdDetalleCompra";
            var p = cmd.CreateParameter(); p.ParameterName = "@Token"; p.Value = token; cmd.Parameters.Add(p);
            await using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync()) lineas.Add(new LineaMovil
            {
                IdDetalleCompra = rd.GetInt32(0),
                IdMaterial = rd.GetInt32(1),
                IdPresentacion = rd.IsDBNull(2) ? null : rd.GetInt32(2),
                CodigoBarras = rd.GetString(3),
                Material = rd.GetString(4),
                Presentacion = rd.GetString(5),
                Esperada = rd.GetDecimal(6),
                Contada = rd.GetDecimal(7)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al consultar líneas de recepción QR.");
            return StatusCode(500, "No se pudo consultar el checklist.");
        }
        finally { if (abrir && conexion.State == ConnectionState.Open) await conexion.CloseAsync(); }
        if (lineas.Count == 0) return BadRequest("Vincula primero este QR a un checklist abierto desde la PC.");
        ViewBag.Token = token;
        return View(lineas);
    }

    public sealed class LineaMovil
    {
        public int IdDetalleCompra { get; set; }
        public int IdMaterial { get; set; }
        public int? IdPresentacion { get; set; }
        public string CodigoBarras { get; set; } = "";
        public string Material { get; set; } = "";
        public string Presentacion { get; set; } = "";
        public decimal Esperada { get; set; }
        public decimal Contada { get; set; }
    }

}

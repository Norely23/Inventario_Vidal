using Inventario_Vidal.Data;
using Inventario_Vidal.DTOs;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vidal.Controllers;

[LoginRequerido]
[PermisoRequerido("COMPRA.RECIBIR")]
public class RecepcionQrPcController : Controller
{
    private readonly FabricacionesVidalEirlBdContext _db;
    private readonly IEscaneoMovilService _escaneo;
    private readonly ILogger<RecepcionQrPcController> _logger;

    public RecepcionQrPcController(FabricacionesVidalEirlBdContext db, IEscaneoMovilService escaneo, ILogger<RecepcionQrPcController> logger)
    {
        _db = db;
        _escaneo = escaneo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int id)
    {
        if (id <= 0) return BadRequest();
        var compra = await _db.Compras.AsNoTracking().Where(c => c.IdCompra == id && c.Estado == "Registrada")
            .Select(c => new { c.IdCompra, c.NumeroComprobante, c.IdProveedor }).SingleOrDefaultAsync();
        if (compra == null) return NotFound("La compra no está registrada o no existe.");
        ViewBag.IdCompra = compra.IdCompra;
        ViewBag.NumeroComprobante = compra.NumeroComprobante;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(int id)
    {
        if (id <= 0) return BadRequest();
        var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
        if (!idUsuario.HasValue || idUsuario <= 0) return Unauthorized();
        var compra = await _db.Compras.AsNoTracking().Where(c => c.IdCompra == id && c.Estado == "Registrada")
            .Select(c => new { c.IdCompra, c.IdProveedor, c.NumeroComprobante }).SingleOrDefaultAsync();
        if (compra == null) return NotFound("La compra debe estar registrada.");
        var checklistListo = await _db.Database.SqlQueryRaw<int>(
            "SELECT CAST(COUNT_BIG(*) AS INT) AS Value FROM Compras.RecepcionesChecklist WHERE IdCompra={0} AND Estado='Borrador'", id).SingleAsync();
        if (checklistListo != 1)
        {
            TempData["MensajeError"] = "Primero inicia un checklist abierto para esta compra.";
            return RedirectToAction(nameof(Index), new { id });
        }
        string? token = null;
        try
        {
            token = _escaneo.CrearSesion(idUsuario.Value, TiposProcesoEscaneo.Recepcion, compra.IdProveedor);
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC Compras.sp_SesionQrRecepcion_Vincular @Token={token},@IdCompra={id},@IdUsuario={idUsuario.Value}");
            var enlace = Url.Action("Movil", "RecepcionQr", new { token }, Request.Scheme, Request.Host.Value);
            if (string.IsNullOrWhiteSpace(enlace)) throw new InvalidOperationException("No se pudo crear el enlace móvil.");
            ViewBag.IdCompra = id;
            ViewBag.NumeroComprobante = compra.NumeroComprobante;
            ViewBag.EnlaceMovil = enlace;
            return View("Index");
        }
        catch (Exception ex)
        {
            if (token != null) _escaneo.CerrarSesion(token);
            _logger.LogWarning(ex, "Error al preparar recepción QR para compra {IdCompra}", id);
            TempData["MensajeError"] = "No se pudo crear o vincular la sesión QR. Comprueba que el checklist esté abierto.";
            return RedirectToAction(nameof(Index), new { id });
        }
    }
}

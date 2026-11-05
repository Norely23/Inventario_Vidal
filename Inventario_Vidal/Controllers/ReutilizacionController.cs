using Inventario_Vidal.Data;
using Inventario_Vidal.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Inventario_Vidal.DTOs.Reutilizacion;
using Inventario_Vidal.Services;

namespace Inventario_Vidal.Controllers;

[LoginRequerido]
public class ReutilizacionController : Controller
{
    private readonly FabricacionesVidalEirlBdContext _db;
    private readonly ILogger<ReutilizacionController> _logger;
    private readonly ReutilizacionConsultaService _consultas;
    public ReutilizacionController(FabricacionesVidalEirlBdContext db, ILogger<ReutilizacionController> logger)
    { _db = db; _logger = logger; _consultas = new ReutilizacionConsultaService(db); }

    [HttpGet, PermisoRequerido("REUTILIZACION.VER")]
    public async Task<IActionResult> Index()
    {
        var filas = new List<MaterialRecuperadoDto>();
        await using var conexion = new SqlConnection(_db.Database.GetConnectionString());
        await conexion.OpenAsync();
        await using var cmd = new SqlCommand(@"SELECT IdRecuperado,Material,CodigoProyecto,CantidadIngresada,
            CantidadDisponible,Estado,FechaRegistro FROM Inventario.vw_MaterialesParaReutilizacion
            ORDER BY FechaRegistro DESC,IdRecuperado DESC", conexion);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            filas.Add(new MaterialRecuperadoDto
            {
                IdRecuperado = reader.GetInt64(0),
                Material = reader.GetString(1),
                CodigoProyecto = reader.GetString(2),
                CantidadIngresada = reader.GetDecimal(3),
                CantidadDisponible = reader.GetDecimal(4),
                Estado = reader.GetString(5),
                FechaRegistro = reader.GetDateTime(6)
            });
        var proyectos = new List<SelectListItem>();
        var receptores = new List<ReceptorOpcionDto>();
        await using (var cmdProy = new SqlCommand(@"SELECT p.IdProyecto,p.CodigoProyecto,COALESCE(p.Descripcion,'')
            FROM Proyectos.Proyectos p JOIN Proyectos.EstadosProyecto ep ON ep.IdEstado=p.IdEstado
            WHERE ep.EsFinal=0 ORDER BY p.CodigoProyecto", conexion))
        await using (var rd = await cmdProy.ExecuteReaderAsync())
            while (await rd.ReadAsync())
                proyectos.Add(new SelectListItem { Value = rd.GetInt32(0).ToString(), Text = rd.GetString(1) + " - " + rd.GetString(2) });
        await using (var cmdReceptor = new SqlCommand(@"SELECT a.IdProyecto,'E:'+CONVERT(VARCHAR(20),e.IdEmpleado),e.NombreCompleto
            FROM Proyectos.AsignacionPersonalProyecto a JOIN Empleados.Empleados e ON e.IdEmpleado=a.IdEmpleado
            WHERE a.FechaHoraFin IS NULL AND e.Activo=1
            UNION ALL
            SELECT a.IdProyecto,'C:'+CONVERT(VARCHAR(20),c.IdContratista),c.NombreCompleto
            FROM Proyectos.AsignacionPersonalProyecto a JOIN Contratistas.Contratistas c ON c.IdContratista=a.IdContratista
            WHERE a.FechaHoraFin IS NULL AND c.Activo=1 ORDER BY 3", conexion))
        await using (var rd = await cmdReceptor.ExecuteReaderAsync())
            while (await rd.ReadAsync())
                receptores.Add(new ReceptorOpcionDto { IdProyecto = rd.GetInt32(0), Valor = rd.GetString(1), Nombre = rd.GetString(2) });
        ViewBag.Proyectos = proyectos;
        ViewBag.Receptores = receptores;
        var idUsuario = HttpContext.Session.GetInt32("IdUsuario") ?? 0;
        await using (var cmdPerm = new SqlCommand(@"SELECT COUNT(1) FROM Seguridad.Usuarios u
            JOIN Seguridad.Roles r ON r.IdRol=u.IdRol
            JOIN Seguridad.RolPermiso rp ON rp.IdRol=r.IdRol
            JOIN Seguridad.Permisos p ON p.IdPermiso=rp.IdPermiso
            WHERE u.IdUsuario=@usuario AND u.Activo=1 AND r.Activo=1 AND p.Activo=1
            AND p.Codigo='REUTILIZACION.GESTIONAR'", conexion))
        {
            cmdPerm.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
            ViewBag.PuedeGestionar = Convert.ToInt32(await cmdPerm.ExecuteScalarAsync()) > 0;
        }
        return View(filas);
    }

    [HttpGet, PermisoRequerido("REUTILIZACION.VER")]
    public async Task<IActionResult> Historial(long? idRecuperado)
    {
        if (idRecuperado.HasValue && idRecuperado.Value <= 0) return BadRequest();
        var historial = await _consultas.ObtenerHistorialAsync(idRecuperado);
        return View(historial);
    }

    [HttpPost, ValidateAntiForgeryToken, PermisoRequerido("REUTILIZACION.GESTIONAR")]
    public async Task<IActionResult> Inspeccionar(long id, bool apto, string? observacion)
    {
        var usuario = HttpContext.Session.GetInt32("IdUsuario");
        if (usuario is null || usuario <= 0) return Unauthorized();
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"EXEC Inventario.sp_Recuperado_Inspeccionar
                @IdRecuperado={id},@Apto={apto},@IdUsuario={usuario.Value},@Observacion={observacion}");
            TempData["MensajeExito"] = "Inspección registrada";
        }
        catch (Exception ex)
        { _logger.LogError(ex, "Fallo al inspeccionar recuperado {Id}", id); TempData["MensajeError"] = "No se pudo registrar la inspección"; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, PermisoRequerido("REUTILIZACION.GESTIONAR")]
    public async Task<IActionResult> Reasignar(long id, decimal cantidad, int idProyectoDestino, string receptor, string? observacion)
    {
        var usuario = HttpContext.Session.GetInt32("IdUsuario");
        if (usuario is null || usuario <= 0) return Unauthorized();
        if (cantidad <= 0 || idProyectoDestino <= 0 || string.IsNullOrWhiteSpace(receptor))
        { TempData["MensajeError"] = "Indique proyecto, receptor y cantidad válidos"; return RedirectToAction(nameof(Index)); }
        var partes = receptor.Split(':', 2);
        if (partes.Length != 2 || !int.TryParse(partes[1], out var idReceptor) || idReceptor <= 0 || (partes[0] != "E" && partes[0] != "C"))
        { TempData["MensajeError"] = "Receptor no válido"; return RedirectToAction(nameof(Index)); }
        int? idEmpleado = partes[0] == "E" ? idReceptor : null;
        int? idContratista = partes[0] == "C" ? idReceptor : null;
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"EXEC Inventario.sp_Recuperado_Reasignar
                @IdRecuperado={id},@Cantidad={cantidad},@IdProyectoDestino={idProyectoDestino},
                @IdUsuario={usuario.Value},@Observacion={observacion},@IdEmpleadoDestino={idEmpleado},@IdContratistaDestino={idContratista}");
            TempData["MensajeExito"] = "Material recuperado reasignado";
        }
        catch (Exception ex)
        { _logger.LogError(ex, "Fallo al reasignar recuperado {Id}", id); TempData["MensajeError"] = "No se pudo completar la reasignación"; }
        return RedirectToAction(nameof(Index));
    }
}

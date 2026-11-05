using Inventario_Vidal.Data;
using Inventario_Vidal.Filters;
using Inventario_Vidal.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Inventario_Vidal.Controllers;

[LoginRequerido]
public class ConsultaDocumentoController : Controller
{
    private const int MaximoConsultasPorMinuto = 20;

    private static readonly string[] PermisosPermitidos =
    {
        "CLIENTE.GESTIONAR",
        "PROVEEDOR.CREAR",
        "PROVEEDOR.EDITAR",
        "EMPLEADO.GESTIONAR",
        "CONTRATISTA.GESTIONAR"
    };

    private readonly IConsultaDocumentoService _consultaService;
    private readonly FabricacionesVidalEirlBdContext _context;
    private readonly IMemoryCache _cache;

    public ConsultaDocumentoController(
        IConsultaDocumentoService consultaService,
        FabricacionesVidalEirlBdContext context,
        IMemoryCache cache)
    {
        _consultaService = consultaService;
        _context = context;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar(string tipo, string numero, CancellationToken cancellationToken)
    {
        var usuarioId = HttpContext.Session.GetInt32("IdUsuario");

        if (!usuarioId.HasValue || usuarioId.Value <= 0)
        {
            return Unauthorized();
        }

        var autorizado = await _context.Usuarios.AsNoTracking().AnyAsync(u =>
            u.IdUsuario == usuarioId.Value &&
            u.Activo &&
            u.IdRolNavigation.Activo &&
            u.IdRolNavigation.IdPermisos.Any(p => p.Activo && PermisosPermitidos.Contains(p.Codigo)),
            cancellationToken);

        if (!autorizado)
        {
            return Forbid();
        }

        var contador = _cache.GetOrCreate(
            $"consulta-documento:{usuarioId.Value}:{DateTime.UtcNow:yyyyMMddHHmm}",
            entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return new int[1];
            })!;

        if (Interlocked.Increment(ref contador[0]) > MaximoConsultasPorMinuto)
        {
            return Json(new
            {
                encontrado = false,
                estado = "LimiteExcedido",
                mensaje = "Demasiadas consultas seguidas. Espera un momento o ingresa los datos manualmente."
            });
        }

        var resultado = await _consultaService.ConsultarAsync(tipo, numero, cancellationToken);

        return Json(new
        {
            encontrado = resultado.Encontrado,
            estado = resultado.Estado.ToString(),
            mensaje = resultado.Mensaje,
            advertencia = resultado.Advertencia,
            tipo = resultado.TipoDocumento,
            numero = resultado.NumeroDocumento,
            nombre = resultado.NombreORazonSocial,
            direccion = resultado.Direccion,
            distrito = resultado.Distrito,
            provincia = resultado.Provincia,
            departamento = resultado.Departamento,
            estadoContribuyente = resultado.EstadoContribuyente,
            condicion = resultado.Condicion,
            desdeCache = resultado.DesdeCache
        });
    }
}

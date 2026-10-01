using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string NombreUsuario { get; set; } = null!;

    public string? CorreoElectronico { get; set; }

    public string ContrasenaHash { get; set; } = null!;

    public int IdRol { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public virtual ICollection<AsignacionMaterialesPersonal> AsignacionMaterialesPersonals { get; set; } = new List<AsignacionMaterialesPersonal>();

    public virtual ICollection<AsignacionPersonalProyecto> AsignacionPersonalProyectos { get; set; } = new List<AsignacionPersonalProyecto>();

    public virtual ICollection<Auditorium> Auditoria { get; set; } = new List<Auditorium>();

    public virtual ICollection<AuditoriaStock> AuditoriaStocks { get; set; } = new List<AuditoriaStock>();

    public virtual ICollection<AuthToken> AuthTokens { get; set; } = new List<AuthToken>();

    public virtual ICollection<BitacoraAcceso> BitacoraAccesos { get; set; } = new List<BitacoraAcceso>();

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual ICollection<CostosProyecto> CostosProyectos { get; set; } = new List<CostosProyecto>();

    public virtual ICollection<HistorialCodigosBarra> HistorialCodigosBarras { get; set; } = new List<HistorialCodigosBarra>();

    public virtual Role IdRolNavigation { get; set; } = null!;

    public virtual ICollection<KardexInventario> KardexInventarios { get; set; } = new List<KardexInventario>();

    public virtual ICollection<MantenimientoMaquinarium> MantenimientoMaquinaria { get; set; } = new List<MantenimientoMaquinarium>();

    public virtual ICollection<PlanosProyecto> PlanosProyectos { get; set; } = new List<PlanosProyecto>();

    public virtual ICollection<PreciosProveedor> PreciosProveedors { get; set; } = new List<PreciosProveedor>();

    public virtual ICollection<PrestamosMaquinarium> PrestamosMaquinaria { get; set; } = new List<PrestamosMaquinarium>();

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();

    public virtual ICollection<TiposCambio> TiposCambios { get; set; } = new List<TiposCambio>();
}

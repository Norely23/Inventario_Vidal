using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Proyecto
{
    public int IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public int IdCliente { get; set; }

    public int IdTipoProyecto { get; set; }

    public int IdEstado { get; set; }

    public string? Descripcion { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaEntregaEstimada { get; set; }

    public DateOnly? FechaEntregaReal { get; set; }

    public decimal? PrecioVenta { get; set; }

    public decimal CostoEstimado { get; set; }

    public decimal? CostoReal { get; set; }

    public string? Moneda { get; set; }

    public int IdUsuarioResponsable { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaActualizacion { get; set; }

    public string? Observaciones { get; set; }

    public string? Prioridad { get; set; }

    public virtual ICollection<AsignacionMaterialesPersonal> AsignacionMaterialesPersonals { get; set; } = new List<AsignacionMaterialesPersonal>();

    public virtual ICollection<AsignacionPersonalProyecto> AsignacionPersonalProyectos { get; set; } = new List<AsignacionPersonalProyecto>();

    public virtual ICollection<CostosProyecto> CostosProyectos { get; set; } = new List<CostosProyecto>();

    public virtual Cliente IdClienteNavigation { get; set; } = null!;

    public virtual EstadosProyecto IdEstadoNavigation { get; set; } = null!;

    public virtual TiposProyecto IdTipoProyectoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioResponsableNavigation { get; set; } = null!;

    public virtual ICollection<KardexInventario> KardexInventarios { get; set; } = new List<KardexInventario>();

    public virtual ICollection<PlanosProyecto> PlanosProyectos { get; set; } = new List<PlanosProyecto>();

    public virtual ICollection<PrestamosMaquinarium> PrestamosMaquinaria { get; set; } = new List<PrestamosMaquinarium>();
}

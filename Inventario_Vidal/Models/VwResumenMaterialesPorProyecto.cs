using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwResumenMaterialesPorProyecto
{
    public int IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public string? ProyectoDescripcion { get; set; }

    public int? TotalAsignaciones { get; set; }

    public int? MaterialesDiferentes { get; set; }

    public decimal? TotalEntregado { get; set; }

    public decimal? TotalUtilizado { get; set; }

    public decimal? TotalDevuelto { get; set; }

    public decimal? TotalDanado { get; set; }

    public decimal? TotalPendiente { get; set; }
}

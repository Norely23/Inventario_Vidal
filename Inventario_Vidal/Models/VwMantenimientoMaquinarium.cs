using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwMantenimientoMaquinarium
{
    public int IdMantenimiento { get; set; }

    public int IdMaquinaria { get; set; }

    public string CodigoMaquinaria { get; set; } = null!;

    public string Maquinaria { get; set; } = null!;

    public string TipoMantenimiento { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public string? Descripcion { get; set; }

    public decimal Costo { get; set; }

    public string? Moneda { get; set; }

    public string RegistradoPor { get; set; } = null!;

    public string? Observaciones { get; set; }

    public decimal? CostoPen { get; set; }
}

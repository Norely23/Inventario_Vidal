using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwCostosProyecto
{
    public int IdCosto { get; set; }

    public int IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public string TipoCosto { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? Moneda { get; set; }

    public DateOnly Fecha { get; set; }

    public string RegistradoPor { get; set; } = null!;

    public decimal? MontoPen { get; set; }
}

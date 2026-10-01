using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwResumenInventarioPorCategorium
{
    public int IdCategoria { get; set; }

    public string Categoria { get; set; } = null!;

    public int? TotalMateriales { get; set; }

    public decimal? StockTotal { get; set; }

    public decimal? StockTotalFormateado { get; set; }

    public int? MaterialesBajoStock { get; set; }

    public double? PorcentajeBajoStock { get; set; }

    public int? MaterialesSinStock { get; set; }
}

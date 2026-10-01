using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwDesglosePaquetesMaterial
{
    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Material { get; set; } = null!;

    public decimal CapacidadUnidadBase { get; set; }

    public string Presentacion { get; set; } = null!;

    public int? PaquetesSellados { get; set; }

    public decimal? UnidadesEnSellados { get; set; }

    public int? PaquetesAbiertos { get; set; }

    public decimal? UnidadesEnAbiertos { get; set; }

    public decimal? TotalUnidadesEnPaquetes { get; set; }

    public decimal StockActualMaterial { get; set; }
}

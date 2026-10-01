using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwUltimosMovimientosKardex
{
    public long IdMovimiento { get; set; }

    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Material { get; set; } = null!;

    public string TipoMovimiento { get; set; } = null!;

    public string? Movimiento { get; set; }

    public decimal? StockAnterior { get; set; }

    public decimal? StockPosterior { get; set; }

    public string Usuario { get; set; } = null!;

    public DateTime FechaMovimiento { get; set; }

    public string? Observacion { get; set; }
}

using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwKardexDetallado
{
    public long IdMovimiento { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Material { get; set; } = null!;

    public string TipoMovimiento { get; set; } = null!;

    public short Signo { get; set; }

    public decimal Cantidad { get; set; }

    public decimal? StockAnterior { get; set; }

    public decimal? StockPosterior { get; set; }

    public decimal? CantidadUnidadOrigen { get; set; }

    public string? UnidadOrigen { get; set; }

    public string? CodigoProyecto { get; set; }

    public string? Contratista { get; set; }

    public string? Empleado { get; set; }

    public string? Comprobante { get; set; }

    public string RegistradoPor { get; set; } = null!;

    public DateTime FechaMovimiento { get; set; }

    public string? Observacion { get; set; }
}

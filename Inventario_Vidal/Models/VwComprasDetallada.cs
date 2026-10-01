using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwComprasDetallada
{
    public int IdCompra { get; set; }

    public string NumeroComprobante { get; set; } = null!;

    public string TipoComprobante { get; set; } = null!;

    public string Proveedor { get; set; } = null!;

    public string Material { get; set; } = null!;

    public decimal Cantidad { get; set; }

    public string? Presentacion { get; set; }

    public decimal? FactorConversionUsado { get; set; }

    public decimal? CantidadUnidadBase { get; set; }

    public decimal CostoUnitario { get; set; }

    public string? Moneda { get; set; }

    public decimal? Subtotal { get; set; }

    public DateTime FechaCompra { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime? FechaRecepcion { get; set; }

    public decimal? CostoUnitarioPen { get; set; }
}

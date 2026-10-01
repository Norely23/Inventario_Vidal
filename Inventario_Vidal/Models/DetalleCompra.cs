using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class DetalleCompra
{
    public int IdDetalleCompra { get; set; }

    public int IdCompra { get; set; }

    public int IdMaterial { get; set; }

    public decimal Cantidad { get; set; }

    public int? IdUnidadCompra { get; set; }

    public decimal CostoUnitario { get; set; }

    public string? Moneda { get; set; }

    public decimal? Subtotal { get; set; }

    public string? Lote { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public decimal? CantidadRecibida { get; set; }

    public int? IdPresentacion { get; set; }

    public decimal? FactorConversionUsado { get; set; }

    public decimal? CantidadUnidadBase { get; set; }

    public virtual Compra IdCompraNavigation { get; set; } = null!;

    public virtual Materiale IdMaterialNavigation { get; set; } = null!;

    public virtual PresentacionesMaterial? IdPresentacionNavigation { get; set; }

    public virtual UnidadesMedidum? IdUnidadCompraNavigation { get; set; }

    public virtual ICollection<PaquetesInventario> PaquetesInventarios { get; set; } = new List<PaquetesInventario>();
}

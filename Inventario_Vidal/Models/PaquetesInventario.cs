using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class PaquetesInventario
{
    public long IdPaquete { get; set; }

    public int IdMaterial { get; set; }

    public int? IdDetalleCompra { get; set; }

    public int? IdPresentacion { get; set; }

    public string? NombrePresentacion { get; set; }

    public decimal CapacidadUnidadBase { get; set; }

    public decimal CantidadActualUnidadBase { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaIngreso { get; set; }

    public DateTime? FechaApertura { get; set; }

    public DateTime? FechaAgotamiento { get; set; }

    public virtual DetalleCompra? IdDetalleCompraNavigation { get; set; }

    public virtual Materiale IdMaterialNavigation { get; set; } = null!;

    public virtual PresentacionesMaterial? IdPresentacionNavigation { get; set; }
}

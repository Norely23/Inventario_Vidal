using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class MaterialProveedor
{
    public int IdMaterialProveedor { get; set; }

    public int IdMaterial { get; set; }

    public int IdProveedor { get; set; }

    public decimal CostoUnitario { get; set; }

    public string? Moneda { get; set; }

    public string? CodigoProveedor { get; set; }

    public bool EsProveedorPrincipal { get; set; }

    public DateTime FechaActualizacion { get; set; }

    public decimal? PrecioMayorRef { get; set; }

    public decimal? PrecioMenorRef { get; set; }

    public decimal? UltimoPrecioCompra { get; set; }

    public DateOnly? FechaUltimoPrecio { get; set; }

    public virtual Materiale IdMaterialNavigation { get; set; } = null!;

    public virtual Proveedore IdProveedorNavigation { get; set; } = null!;

    public virtual ICollection<PreciosProveedor> PreciosProveedors { get; set; } = new List<PreciosProveedor>();
}

using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Compra
{
    public int IdCompra { get; set; }

    public string NumeroComprobante { get; set; } = null!;

    public string TipoComprobante { get; set; } = null!;

    public int IdProveedor { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaCompra { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Igv { get; set; }

    public decimal Total { get; set; }

    public string? Moneda { get; set; }

    public string Estado { get; set; } = null!;

    public string? Observaciones { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual Proveedore IdProveedorNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<KardexInventario> KardexInventarios { get; set; } = new List<KardexInventario>();
}

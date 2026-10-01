using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwMaterialesConProveedor
{
    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal StockReservado { get; set; }

    public decimal? StockDisponible { get; set; }

    public bool Activo { get; set; }

    public string Categoria { get; set; } = null!;

    public string? Marca { get; set; }

    public string? ProveedorPrincipal { get; set; }

    public string? ProveedorRuc { get; set; }

    public decimal? UltimoCosto { get; set; }

    public string? MonedaCosto { get; set; }

    public decimal? UltimoPrecioCompra { get; set; }

    public string EstadoStock { get; set; } = null!;
}

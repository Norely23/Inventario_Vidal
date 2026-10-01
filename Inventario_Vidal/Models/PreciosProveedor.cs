using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class PreciosProveedor
{
    public int IdPrecioProveedor { get; set; }

    public int IdMaterialProveedor { get; set; }

    public decimal CostoUnitario { get; set; }

    public string Moneda { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public bool EsVigente { get; set; }

    public string TipoPrecio { get; set; } = null!;

    public int UsuarioRegistro { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string? Observaciones { get; set; }

    public virtual MaterialProveedor IdMaterialProveedorNavigation { get; set; } = null!;

    public virtual Usuario UsuarioRegistroNavigation { get; set; } = null!;
}

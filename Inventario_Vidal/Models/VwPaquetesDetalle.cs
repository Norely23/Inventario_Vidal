using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwPaquetesDetalle
{
    public long IdPaquete { get; set; }

    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Material { get; set; } = null!;

    public string Presentacion { get; set; } = null!;

    public decimal CapacidadUnidadBase { get; set; }

    public decimal CantidadActualUnidadBase { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaIngreso { get; set; }

    public DateTime? FechaApertura { get; set; }

    public DateTime? FechaAgotamiento { get; set; }
}

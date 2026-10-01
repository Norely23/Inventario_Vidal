using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwPaquetesInconsistencia
{
    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public decimal StockActual { get; set; }

    public decimal TotalEnPaquetes { get; set; }

    public decimal? Diferencia { get; set; }
}

using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class AuditoriaStock
{
    public long IdAuditoriaStock { get; set; }

    public long IdMovimientoKardex { get; set; }

    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string NombreMaterial { get; set; } = null!;

    public string TipoMovimiento { get; set; } = null!;

    public decimal Cantidad { get; set; }

    public decimal StockAnterior { get; set; }

    public decimal StockPosterior { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

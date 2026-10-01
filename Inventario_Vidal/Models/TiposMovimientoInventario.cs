using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class TiposMovimientoInventario
{
    public int IdTipoMovimiento { get; set; }

    public string Nombre { get; set; } = null!;

    public short Signo { get; set; }

    public string? Clasificacion { get; set; }

    public virtual ICollection<KardexInventario> KardexInventarios { get; set; } = new List<KardexInventario>();
}

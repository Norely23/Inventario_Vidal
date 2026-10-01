using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class HistorialCodigosBarra
{
    public long IdHistorial { get; set; }

    public int IdMaterial { get; set; }

    public string CodigoBarrasViejo { get; set; } = null!;

    public string CodigoBarrasNuevo { get; set; } = null!;

    public int IdUsuario { get; set; }

    public DateTime FechaCambio { get; set; }

    public string? Motivo { get; set; }

    public virtual Materiale IdMaterialNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

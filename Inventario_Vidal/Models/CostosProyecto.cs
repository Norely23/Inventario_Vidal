using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class CostosProyecto
{
    public int IdCosto { get; set; }

    public int IdProyecto { get; set; }

    public string TipoCosto { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public decimal Monto { get; set; }

    public string? Moneda { get; set; }

    public DateOnly Fecha { get; set; }

    public int IdUsuario { get; set; }

    public virtual Proyecto IdProyectoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

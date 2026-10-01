using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class MantenimientoMaquinarium
{
    public int IdMantenimiento { get; set; }

    public int IdMaquinaria { get; set; }

    public string TipoMantenimiento { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public string? Descripcion { get; set; }

    public decimal Costo { get; set; }

    public string? Moneda { get; set; }

    public int IdUsuario { get; set; }

    public string? Observaciones { get; set; }

    public virtual Maquinaria IdMaquinariaNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

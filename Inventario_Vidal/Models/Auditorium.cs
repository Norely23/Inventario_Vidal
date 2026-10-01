using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Auditorium
{
    public long IdAuditoria { get; set; }

    public int? IdUsuario { get; set; }

    public string NombreTabla { get; set; } = null!;

    public string Accion { get; set; } = null!;

    public string? IdRegistroAfectado { get; set; }

    public string? ValoresAnteriores { get; set; }

    public string? ValoresNuevos { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}

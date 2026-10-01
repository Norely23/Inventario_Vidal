using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class PlanosProyecto
{
    public int IdPlano { get; set; }

    public int IdProyecto { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string RutaArchivo { get; set; } = null!;

    public int Version { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaSubida { get; set; }

    public string? Observaciones { get; set; }

    public bool EsVersionActual { get; set; }

    public virtual Proyecto IdProyectoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

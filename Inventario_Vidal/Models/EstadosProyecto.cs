using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class EstadosProyecto
{
    public int IdEstado { get; set; }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }

    public bool EsFinal { get; set; }

    public string? ColorHex { get; set; }

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}

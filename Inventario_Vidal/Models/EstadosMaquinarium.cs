using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class EstadosMaquinarium
{
    public int IdEstadoMaquinaria { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Maquinaria> Maquinaria { get; set; } = new List<Maquinaria>();
}

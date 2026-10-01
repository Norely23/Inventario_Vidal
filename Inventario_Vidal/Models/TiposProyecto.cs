using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class TiposProyecto
{
    public int IdTipoProyecto { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}

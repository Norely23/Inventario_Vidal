using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class UnidadesMedidum
{
    public int IdUnidad { get; set; }

    public string Nombre { get; set; } = null!;

    public string Abreviatura { get; set; } = null!;

    public bool EsFraccionable { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual ICollection<KardexInventario> KardexInventarios { get; set; } = new List<KardexInventario>();

    public virtual ICollection<Materiale> MaterialeIdUnidadCompraNavigations { get; set; } = new List<Materiale>();

    public virtual ICollection<Materiale> MaterialeIdUnidadConsumoNavigations { get; set; } = new List<Materiale>();

    public virtual ICollection<PresentacionesMaterial> PresentacionesMaterials { get; set; } = new List<PresentacionesMaterial>();
}

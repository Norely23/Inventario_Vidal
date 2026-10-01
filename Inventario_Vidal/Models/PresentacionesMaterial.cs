using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class PresentacionesMaterial
{
    public int IdPresentacion { get; set; }

    public int IdMaterial { get; set; }

    public string Nombre { get; set; } = null!;

    public int IdUnidadPresentacion { get; set; }

    public decimal FactorAunidadBase { get; set; }

    public bool EsPredeterminada { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual Materiale IdMaterialNavigation { get; set; } = null!;

    public virtual UnidadesMedidum IdUnidadPresentacionNavigation { get; set; } = null!;

    public virtual ICollection<PaquetesInventario> PaquetesInventarios { get; set; } = new List<PaquetesInventario>();
}

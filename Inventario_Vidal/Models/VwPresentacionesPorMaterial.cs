using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwPresentacionesPorMaterial
{
    public int IdPresentacion { get; set; }

    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Material { get; set; } = null!;

    public string Presentacion { get; set; } = null!;

    public string UnidadPresentacion { get; set; } = null!;

    public decimal FactorAunidadBase { get; set; }

    public bool EsPredeterminada { get; set; }

    public bool Activo { get; set; }

    public decimal? PresentacionesCompletasEquivalentes { get; set; }
}

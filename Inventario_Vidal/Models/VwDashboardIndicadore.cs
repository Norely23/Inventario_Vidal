using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwDashboardIndicadore
{
    public int? AlertasStockBajo { get; set; }

    public int? MaterialesSinStock { get; set; }

    public int? ProyectosEnProceso { get; set; }

    public int? ProyectosAtrasados { get; set; }

    public decimal? ComprasUltimoMes { get; set; }

    public int? MaquinariaEnPrestamo { get; set; }

    public int? UsuariosActivos { get; set; }

    public DateTime FechaActualizacion { get; set; }
}

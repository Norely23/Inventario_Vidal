using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class PrestamosMaquinarium
{
    public int IdPrestamo { get; set; }

    public int IdMaquinaria { get; set; }

    public int? IdProyecto { get; set; }

    public int? IdContratista { get; set; }

    public int? IdEmpleado { get; set; }

    public DateTime FechaPrestamo { get; set; }

    public DateOnly? FechaDevolucionEstimada { get; set; }

    public DateTime? FechaDevolucionReal { get; set; }

    public string Estado { get; set; } = null!;

    public int IdUsuario { get; set; }

    public string? Observaciones { get; set; }

    public virtual Contratista? IdContratistaNavigation { get; set; }

    public virtual Empleado? IdEmpleadoNavigation { get; set; }

    public virtual Maquinaria IdMaquinariaNavigation { get; set; } = null!;

    public virtual Proyecto? IdProyectoNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

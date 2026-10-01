using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class AsignacionPersonalProyecto
{
    public int IdAsignacion { get; set; }

    public int IdProyecto { get; set; }

    public int? IdEmpleado { get; set; }

    public int? IdContratista { get; set; }

    public string? RolEnProyecto { get; set; }

    public DateTime FechaHoraInicio { get; set; }

    public DateTime? FechaHoraFin { get; set; }

    public string? Motivo { get; set; }

    public int IdUsuarioRegistro { get; set; }

    public virtual Contratista? IdContratistaNavigation { get; set; }

    public virtual Empleado? IdEmpleadoNavigation { get; set; }

    public virtual Proyecto IdProyectoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioRegistroNavigation { get; set; } = null!;
}

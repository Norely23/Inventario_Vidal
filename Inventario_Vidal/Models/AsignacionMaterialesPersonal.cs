using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class AsignacionMaterialesPersonal
{
    public int IdAsignacion { get; set; }

    public int IdProyecto { get; set; }

    public int? IdEmpleado { get; set; }

    public int? IdContratista { get; set; }

    public int IdMaterial { get; set; }

    public decimal CantidadSolicitada { get; set; }

    public decimal CantidadEntregada { get; set; }

    public decimal CantidadUtilizada { get; set; }

    public decimal CantidadDevuelta { get; set; }

    public decimal CantidadDanada { get; set; }

    public decimal CostoUnitarioRef { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public DateOnly? FechaDevolucionEstimada { get; set; }

    public DateTime? FechaDevolucionReal { get; set; }

    public string Estado { get; set; } = null!;

    public int IdUsuarioRegistro { get; set; }

    public string? Observaciones { get; set; }

    public virtual Contratista? IdContratistaNavigation { get; set; }

    public virtual Empleado? IdEmpleadoNavigation { get; set; }

    public virtual Materiale IdMaterialNavigation { get; set; } = null!;

    public virtual Proyecto IdProyectoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioRegistroNavigation { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Empleado
{
    public int IdEmpleado { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string TipoDocumento { get; set; } = null!;

    public string NumeroDocumento { get; set; } = null!;

    public string Especialidad { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? Direccion { get; set; }

    public DateOnly? FechaIngreso { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<AsignacionMaterialesPersonal> AsignacionMaterialesPersonals { get; set; } = new List<AsignacionMaterialesPersonal>();

    public virtual AsignacionPersonalProyecto? AsignacionPersonalProyecto { get; set; }

    public virtual ICollection<KardexInventario> KardexInventarios { get; set; } = new List<KardexInventario>();

    public virtual ICollection<PrestamosMaquinarium> PrestamosMaquinaria { get; set; } = new List<PrestamosMaquinarium>();
}

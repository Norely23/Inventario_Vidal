using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Proveedore
{
    public int IdProveedor { get; set; }

    public string Ruc { get; set; } = null!;

    public string RazonSocial { get; set; } = null!;

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? ContactoNombre { get; set; }

    public string? CondicionesPago { get; set; }

    public string? Observaciones { get; set; }

    public string? Clasificacion { get; set; }

    public int? Calificacion { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual ICollection<MaterialProveedor> MaterialProveedors { get; set; } = new List<MaterialProveedor>();
}

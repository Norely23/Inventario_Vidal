using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Cliente
{
    public int IdCliente { get; set; }

    public string TipoCliente { get; set; } = null!;

    public string TipoDocumento { get; set; } = null!;

    public string NumeroDocumento { get; set; } = null!;

    public string NombreOrazonSocial { get; set; } = null!;

    public string? NombreComercial { get; set; }

    public string? Direccion { get; set; }

    public string? Distrito { get; set; }

    public string? Provincia { get; set; }

    public string? Departamento { get; set; }

    public string? Telefono { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? ContactoNombre { get; set; }

    public string? Observaciones { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}

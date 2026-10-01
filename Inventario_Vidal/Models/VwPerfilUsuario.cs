using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwPerfilUsuario
{
    public int IdUsuario { get; set; }

    public string NombreCompleto { get; set; } = null!;

    public string NombreUsuario { get; set; } = null!;

    public string? CorreoElectronico { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public int IntentosFallidos { get; set; }

    public DateTime? BloqueadoHasta { get; set; }

    public int IdRol { get; set; }

    public string NombreRol { get; set; } = null!;

    public string? RolDescripcion { get; set; }
}

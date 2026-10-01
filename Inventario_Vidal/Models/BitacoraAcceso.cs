using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class BitacoraAcceso
{
    public long IdBitacora { get; set; }

    public int? UsuarioId { get; set; }

    public string? NombreUsuario { get; set; }

    public string Accion { get; set; } = null!;

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }

    public bool Exitoso { get; set; }

    public DateTime FechaHora { get; set; }

    public virtual Usuario? Usuario { get; set; }
}

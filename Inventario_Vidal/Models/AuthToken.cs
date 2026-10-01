using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class AuthToken
{
    public int TokenId { get; set; }

    public int UsuarioId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public bool Activo { get; set; }

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }

    public string? Dispositivo { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}

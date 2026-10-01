using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwSesionesActiva
{
    public int TokenId { get; set; }

    public int UsuarioId { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string NombreCompleto { get; set; } = null!;

    public string NombreRol { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public int? HorasRestantes { get; set; }

    public string? Ip { get; set; }

    public string? UserAgent { get; set; }

    public string? Dispositivo { get; set; }

    public string EstadoSesion { get; set; } = null!;
}

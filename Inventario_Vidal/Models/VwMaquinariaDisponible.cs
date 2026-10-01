using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwMaquinariaDisponible
{
    public int IdMaquinaria { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? ImagenUrl { get; set; }

    public string Estado { get; set; } = null!;
}

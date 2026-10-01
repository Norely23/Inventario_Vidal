using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Maquinaria
{
    public int IdMaquinaria { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? NumeroSerie { get; set; }

    public int IdEstadoMaquinaria { get; set; }

    public DateOnly? FechaAdquisicion { get; set; }

    public decimal? CostoAdquisicion { get; set; }

    public string? Moneda { get; set; }

    public string? ImagenUrl { get; set; }

    public decimal HorasUso { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateOnly? FechaUltimoMantenimiento { get; set; }

    public virtual EstadosMaquinarium IdEstadoMaquinariaNavigation { get; set; } = null!;

    public virtual ICollection<MantenimientoMaquinarium> MantenimientoMaquinaria { get; set; } = new List<MantenimientoMaquinarium>();

    public virtual ICollection<PrestamosMaquinarium> PrestamosMaquinaria { get; set; } = new List<PrestamosMaquinarium>();
}

using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwProyectosResuman
{
    public int IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public string Cliente { get; set; } = null!;

    public string TipoProyecto { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string? EstadoColor { get; set; }

    public string? Prioridad { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaEntregaEstimada { get; set; }

    public DateOnly? FechaEntregaReal { get; set; }

    public decimal? PrecioVenta { get; set; }

    public decimal CostoEstimado { get; set; }

    public decimal? CostoReal { get; set; }

    public string? Moneda { get; set; }

    public decimal? UtilidadEstimada { get; set; }

    public decimal? MargenPorcentual { get; set; }

    public string Responsable { get; set; } = null!;

    public int? DiasTranscurridos { get; set; }

    public string EstadoEntrega { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwAsignacionesActiva
{
    public int IdAsignacion { get; set; }

    public int IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public string? ProyectoDescripcion { get; set; }

    public string TipoPersona { get; set; } = null!;

    public string? NombrePersona { get; set; }

    public string? DocumentoPersona { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Material { get; set; } = null!;

    public decimal CantidadSolicitada { get; set; }

    public decimal CantidadEntregada { get; set; }

    public decimal CantidadUtilizada { get; set; }

    public decimal CantidadDevuelta { get; set; }

    public decimal CantidadDanada { get; set; }

    public decimal? Pendiente { get; set; }

    public decimal CostoUnitarioRef { get; set; }

    public DateTime FechaAsignacion { get; set; }

    public DateOnly? FechaDevolucionEstimada { get; set; }

    public DateTime? FechaDevolucionReal { get; set; }

    public string Estado { get; set; } = null!;

    public string RegistradoPor { get; set; } = null!;

    public string? Observaciones { get; set; }
}

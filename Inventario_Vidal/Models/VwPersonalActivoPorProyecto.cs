using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwPersonalActivoPorProyecto
{
    public int IdAsignacion { get; set; }

    public int IdProyecto { get; set; }

    public string CodigoProyecto { get; set; } = null!;

    public string TipoPersonal { get; set; } = null!;

    public string? NombrePersonal { get; set; }

    public string? NumeroDocumento { get; set; }

    public string? RolEnProyecto { get; set; }

    public DateTime FechaHoraInicio { get; set; }

    public int? MinutosEnProyecto { get; set; }

    public string? Motivo { get; set; }

    public string? RegistradoPor { get; set; }
}

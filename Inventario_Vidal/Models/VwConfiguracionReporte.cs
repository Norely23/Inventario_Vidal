using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwConfiguracionReporte
{
    public string NombreEmpresa { get; set; } = null!;

    public string Ruc { get; set; } = null!;

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }

    public string? CorreoElectronico { get; set; }

    public string? SitioWeb { get; set; }

    public string? LogoUrl { get; set; }

    public string? PiePagina { get; set; }

    public string Moneda { get; set; } = null!;

    public string? MonedaSecundaria { get; set; }

    public decimal? TipoCambio { get; set; }

    public decimal Igv { get; set; }

    public int? ProyectosActivos { get; set; }

    public int? ProyectosEntregados { get; set; }

    public int? AlertasStockBajo { get; set; }

    public int? MaterialesSinStock { get; set; }
}

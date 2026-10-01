using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class VwStockActual
{
    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public string? Marca { get; set; }

    public string UnidadConsumo { get; set; } = null!;

    public decimal StockActual { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public decimal StockReservado { get; set; }

    public decimal? StockDisponible { get; set; }

    public string? ImagenUrl { get; set; }

    public string? PresentacionPredeterminada { get; set; }

    public string? UnidadPresentacionPredeterminada { get; set; }

    public decimal FactorConversionVigente { get; set; }

    public decimal? PresentacionesCompletas { get; set; }

    public decimal? CantidadSueltaEnUnidadBase { get; set; }

    public string EstadoStock { get; set; } = null!;

    public int AlertaStockBajo { get; set; }

    public bool Activo { get; set; }
}

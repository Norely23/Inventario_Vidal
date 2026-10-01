using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class KardexInventario
{
    public long IdMovimiento { get; set; }

    public int IdMaterial { get; set; }

    public int IdTipoMovimiento { get; set; }

    public decimal Cantidad { get; set; }

    public decimal? StockAnterior { get; set; }

    public decimal? StockPosterior { get; set; }

    public int? IdProyecto { get; set; }

    public int? IdCompra { get; set; }

    public int? IdContratista { get; set; }

    public int? IdEmpleado { get; set; }

    public string? DocumentoReferencia { get; set; }

    public string? Observacion { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public decimal? CantidadUnidadOrigen { get; set; }

    public int? IdUnidadOrigen { get; set; }

    public virtual Compra? IdCompraNavigation { get; set; }

    public virtual Contratista? IdContratistaNavigation { get; set; }

    public virtual Empleado? IdEmpleadoNavigation { get; set; }

    public virtual Materiale IdMaterialNavigation { get; set; } = null!;

    public virtual Proyecto? IdProyectoNavigation { get; set; }

    public virtual TiposMovimientoInventario IdTipoMovimientoNavigation { get; set; } = null!;

    public virtual UnidadesMedidum? IdUnidadOrigenNavigation { get; set; }

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}

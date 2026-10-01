using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class Materiale
{
    public int IdMaterial { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int IdCategoria { get; set; }

    public int? IdMarca { get; set; }

    public int IdUnidadCompra { get; set; }

    public int IdUnidadConsumo { get; set; }

    public decimal FactorConversion { get; set; }

    public bool EsReutilizable { get; set; }

    public bool EsHerramienta { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockReservado { get; set; }

    public decimal? PesoUnitario { get; set; }

    public string? Dimensiones { get; set; }

    public string? ImagenUrl { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaUltimaCompra { get; set; }

    public DateTime? FechaUltimoMovimiento { get; set; }

    public virtual ICollection<AsignacionMaterialesPersonal> AsignacionMaterialesPersonals { get; set; } = new List<AsignacionMaterialesPersonal>();

    public virtual ICollection<DetalleCompra> DetalleCompras { get; set; } = new List<DetalleCompra>();

    public virtual ICollection<HistorialCodigosBarra> HistorialCodigosBarras { get; set; } = new List<HistorialCodigosBarra>();

    public virtual Categoria IdCategoriaNavigation { get; set; } = null!;

    public virtual Marca? IdMarcaNavigation { get; set; }

    public virtual UnidadesMedidum IdUnidadCompraNavigation { get; set; } = null!;

    public virtual UnidadesMedidum IdUnidadConsumoNavigation { get; set; } = null!;

    public virtual ICollection<KardexInventario> KardexInventarios { get; set; } = new List<KardexInventario>();

    public virtual ICollection<MaterialProveedor> MaterialProveedors { get; set; } = new List<MaterialProveedor>();

    public virtual ICollection<PaquetesInventario> PaquetesInventarios { get; set; } = new List<PaquetesInventario>();

    public virtual PresentacionesMaterial? PresentacionesMaterial { get; set; }
}

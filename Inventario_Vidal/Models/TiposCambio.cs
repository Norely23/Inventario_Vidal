using System;
using System.Collections.Generic;

namespace Inventario_Vidal.Models;

public partial class TiposCambio
{
    public int IdTipoCambio { get; set; }

    public DateOnly Fecha { get; set; }

    public string MonedaOrigen { get; set; } = null!;

    public string MonedaDestino { get; set; } = null!;

    public decimal Valor { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Usuario? Usuario { get; set; }
}

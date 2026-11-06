namespace Inventario_Vidal.DTOs
{
    /// <summary>
    /// DTO para listar registros de auditoría
    /// </summary>
    public class AuditoriaDto
    {
        public long IdAuditoria { get; set; }
        public int? IdUsuario { get; set; }
        public string? UsuarioNombre { get; set; }
        public string NombreTabla { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string? IdRegistroAfectado { get; set; }
        public string? ValoresAnteriores { get; set; }
        public string? ValoresNuevos { get; set; }
        public DateTime FechaHora { get; set; }
        public string FechaFormateada { get; set; } = string.Empty;

        public string AccionBadge => Accion.ToLower() switch
        {
            "crear" or "insert" or "insertar" => "success",
            "editar" or "update" or "actualizar" => "warning",
            "eliminar" or "delete" or "borrar" => "danger",
            "login" or "iniciar sesion" => "primary",
            "logout" or "cerrar sesion" => "secondary",
            "activar" => "success",
            "desactivar" => "danger",
            _ => "info"
        };

        public string AccionIcono => Accion.ToLower() switch
        {
            "crear" or "insert" or "insertar" => "plus-circle",
            "editar" or "update" or "actualizar" => "edit",
            "eliminar" or "delete" or "borrar" => "trash",
            "login" or "iniciar sesion" => "log-in",
            "logout" or "cerrar sesion" => "log-out",
            "activar" => "check-circle",
            "desactivar" => "x-circle",
            _ => "info"
        };

        public string AccionTexto => Accion.ToUpper();
        public string ResumenCambio => ObtenerResumenCambio();

        private string ObtenerResumenCambio()
        {
            if (string.IsNullOrEmpty(ValoresAnteriores) && string.IsNullOrEmpty(ValoresNuevos))
                return "Sin cambios registrados";

            if (string.IsNullOrEmpty(ValoresAnteriores))
                return "Nuevo registro creado";

            if (string.IsNullOrEmpty(ValoresNuevos))
                return "Registro eliminado";

            return "Registro modificado";
        }
    }

    /// <summary>
    /// DTO para el filtro de auditoría
    /// </summary>
    public class AuditoriaFiltroDto
    {
        public string? Tabla { get; set; }
        public string? Accion { get; set; }
        public int? UsuarioId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;

        public List<string> TablasDisponibles { get; set; } = new();
        public List<string> AccionesDisponibles { get; set; } = new();
        public Dictionary<int, string> UsuariosDisponibles { get; set; } = new();
    }

    /// <summary>
    /// DTO para el resumen de auditoría
    /// </summary>
    public class AuditoriaResumenDto
    {
        public int TotalRegistros { get; set; }
        public Dictionary<string, int> RegistrosPorTabla { get; set; } = new();
        public Dictionary<string, int> RegistrosPorAccion { get; set; } = new();
        public Dictionary<string, int> RegistrosPorUsuario { get; set; } = new();
        public int RegistrosHoy { get; set; }
        public int RegistrosSemana { get; set; }
        public int RegistrosMes { get; set; }
        public DateTime? UltimoRegistro { get; set; }
    }

    // ================================================================
    // AUDITORÍA DE STOCK (desde AuditoriaStock)
    // ================================================================

    public class AuditoriaStockDto
    {
        public long IdAuditoriaStock { get; set; }
        public long IdMovimientoKardex { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string NombreMaterial { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal StockAnterior { get; set; }
        public decimal StockPosterior { get; set; }
        public int IdUsuario { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }

        public string FechaFormateada => FechaHora.ToString("dd/MM/yyyy HH:mm");
        public string TipoBadge => TipoMovimiento.ToLower() switch
        {
            "entrada" => "success",
            "salida" => "warning",
            "ajuste positivo" => "success",
            "ajuste negativo" => "danger",
            _ => "info"
        };
        public decimal Variacion => StockPosterior - StockAnterior;
        public string VariacionSigno => Variacion >= 0 ? "+" : "";
    }

    /// <summary>
    /// DTO para auditoría de stock con filtro
    /// </summary>
    public class AuditoriaStockFiltroDto
    {
        public int? IdMaterial { get; set; }
        public string? TipoMovimiento { get; set; }
        public int? IdUsuario { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// DTO para resumen de auditoría de stock
    /// </summary>
    public class AuditoriaStockResumenDto
    {
        public int TotalMovimientos { get; set; }
        public int TotalEntradas { get; set; }
        public int TotalSalidas { get; set; }
        public int TotalAjustes { get; set; }
        public decimal SumaEntradas { get; set; }
        public decimal SumaSalidas { get; set; }
        public Dictionary<string, int> MovimientosPorTipo { get; set; } = new();
        public Dictionary<int, int> MovimientosPorUsuario { get; set; } = new();
        public int MovimientosHoy { get; set; }
        public int MovimientosSemana { get; set; }
        public int MovimientosMes { get; set; }
        public DateTime? UltimoMovimiento { get; set; }
    }

    /// <summary>
    /// DTO para detalle de auditoría de stock
    /// </summary>
    public class AuditoriaStockDetailDto
    {
        public long IdAuditoriaStock { get; set; }
        public long IdMovimientoKardex { get; set; }
        public int IdMaterial { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string NombreMaterial { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal StockAnterior { get; set; }
        public decimal StockPosterior { get; set; }
        public int IdUsuario { get; set; }
        public string UsuarioNombre { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }

        public string FechaFormateada => FechaHora.ToString("dd/MM/yyyy HH:mm");
        public string TipoBadge => TipoMovimiento.ToLower() switch
        {
            "entrada" => "success",
            "salida" => "warning",
            "ajuste positivo" => "success",
            "ajuste negativo" => "danger",
            _ => "info"
        };
        public decimal Variacion => StockPosterior - StockAnterior;
        public string VariacionSigno => Variacion >= 0 ? "+" : "";
    }
}
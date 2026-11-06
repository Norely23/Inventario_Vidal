namespace Inventario_Vidal.DTOs
{
    /// <summary>
    /// ViewModel completo para el Dashboard
    /// </summary>
    public class DashboardViewModel
    {
        // Tarjetas de resumen (KPI Cards)
        public int TotalMateriales { get; set; }
        public int TotalProyectos { get; set; }
        public int TotalProveedores { get; set; }
        public int TotalClientes { get; set; }
        public int TotalContratistas { get; set; }
        public int TotalEmpleados { get; set; }
        public int TotalMaquinarias { get; set; }
        public int TotalUsuarios { get; set; }

        // Financiero
        public decimal ComprasDelMes { get; set; }
        public decimal ComprasDelAnio { get; set; }
        public int ProyectosActivos { get; set; }
        public int ProyectosCompletados { get; set; }

        // Gráficos
        public List<EstadoCountDto> ProyectosPorEstado { get; set; } = new();
        public List<MaterialUsoDto> MaterialesMasUsados { get; set; } = new();
        public List<ComprasPorMesDto> ComprasPorMes { get; set; } = new();

        // Alertas y tablas recientes
        public List<AlertasStockDto> AlertasStockBajo { get; set; } = new();
        public List<UltimoMovimientoDto> UltimosMovimientos { get; set; } = new();
        public List<ProyectoRecienteDto> ProyectosRecientes { get; set; } = new();
        public List<UsuarioActivoDto> UsuariosActivos { get; set; } = new();

        // Metadata
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
        public int NumeroAlertasStock { get; set; }
        public int NumeroNotificaciones { get; set; }
    }

    /// <summary>
    /// Conteo de proyectos por estado (para gráfico de barras)
    /// </summary>
    public class EstadoCountDto
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public string Color => ObtenerColor(Estado);

        private static string ObtenerColor(string estado)
        {
            return estado.ToLower() switch
            {
                "activo" or "en progreso" or "en proceso de fabricacion" => "primary",
                "completado" or "entregado" or "terminado" => "success",
                "pausado" or "suspendido" => "warning",
                "cancelado" => "danger",
                "en proceso de acabado" => "info",
                "instalacion de electricidad" => "warning",
                _ => "secondary"
            };
        }
    }

    /// <summary>
    /// Materiales más utilizados (para gráfico de torta)
    /// </summary>
    public class MaterialUsoDto
    {
        public int IdMaterial { get; set; }
        public string Material { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; }
        public string Color => ObtenerColor(Material);

        private static string ObtenerColor(string material)
        {
            var colors = new[] { "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b", "#858796", "#6f42c1", "#fd7e14", "#20c9a6", "#5a5c69" };
            var hash = Math.Abs(material.GetHashCode());
            return colors[hash % colors.Length];
        }
    }

    /// <summary>
    /// Compras agrupadas por mes (para gráfico de línea)
    /// </summary>
    public class ComprasPorMesDto
    {
        public string Mes { get; set; } = string.Empty;
        public int MesNumero { get; set; }
        public int Anio { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>
    /// Último movimiento de kardex
    /// </summary>
    public class UltimoMovimientoDto
    {
        public long IdMovimiento { get; set; }
        public int IdMaterial { get; set; }
        public string Material { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string TipoBadge => TipoMovimiento == "Entrada" ? "success" : TipoMovimiento == "Salida" ? "warning" : "info";
        public string Usuario { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy HH:mm");
        public string? Proyecto { get; set; }
        public string? DocumentoReferencia { get; set; }
    }

    /// <summary>
    /// Proyecto reciente (desde VwProyectosResumen)
    /// </summary>
    public class ProyectoRecienteDto
    {
        public int IdProyecto { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string? EstadoColor { get; set; }
        public string? Prioridad { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaEntregaEstimada { get; set; }
        public DateOnly? FechaEntregaReal { get; set; }
        public decimal? PrecioVenta { get; set; }
        public string Responsable { get; set; } = string.Empty;
        public string? Moneda { get; set; }

        public string EstadoBadge => ObtenerBadgeEstado(Estado);

        private static string ObtenerBadgeEstado(string estado)
        {
            return estado.ToLower() switch
            {
                "activo" or "en progreso" or "en proceso de fabricacion" => "primary",
                "completado" or "entregado" or "terminado" => "success",
                "pausado" or "suspendido" => "warning",
                "cancelado" => "danger",
                "en proceso de acabado" => "info",
                "instalacion de electricidad" => "warning",
                _ => "secondary"
            };
        }
    }

    /// <summary>
    /// Usuario activo (para mostrar en el dashboard)
    /// </summary>
    public class UsuarioActivoDto
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreRol { get; set; } = string.Empty;
        public DateTime? UltimoAcceso { get; set; }
        public string UltimoAccesoFormateado => UltimoAcceso?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";
        public string Iniciales => ObtenerIniciales(NombreCompleto);
        public bool EstaEnLinea => UltimoAcceso.HasValue && (DateTime.Now - UltimoAcceso.Value).TotalMinutes < 5;

        private static string ObtenerIniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "U";
            var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0].Substring(0, 1).ToUpper();
            return (partes[0].Substring(0, 1) + partes[^1].Substring(0, 1)).ToUpper();
        }
    }

    /// <summary>
    /// Estadísticas rápidas del dashboard (desde VwDashboardIndicadores)
    /// </summary>
    public class DashboardEstadisticasDto
    {
        public int TotalMateriales { get; set; }
        public int TotalProyectos { get; set; }
        public int ProyectosActivos { get; set; }
        public int TotalClientes { get; set; }
        public int TotalProveedores { get; set; }
        public int AlertasStockBajo { get; set; }
        public decimal ComprasMes { get; set; }
        public int UsuariosActivos { get; set; }
        public int TotalContratistas { get; set; }
        public int TotalEmpleados { get; set; }
        public int TotalMaquinarias { get; set; }

        public int TotalAlertas => AlertasStockBajo;
        public bool TieneAlertas => AlertasStockBajo > 0;
    }

    /// <summary>
    /// DTO para el resumen de indicadores del dashboard (desde VwDashboardIndicadores)
    /// </summary>
    public class DashboardIndicadoresDto
    {
        public int? AlertasStockBajo { get; set; }
        public int? MaterialesSinStock { get; set; }
        public int? ProyectosEnProceso { get; set; }
        public int? ProyectosAtrasados { get; set; }
        public decimal? ComprasUltimoMes { get; set; }
        public int? MaquinariaEnPrestamo { get; set; }
        public int? UsuariosActivos { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    /// <summary>
    /// DTO para el resumen de inventario por categoría (desde VwResumenInventarioPorCategoria)
    /// </summary>
    public class ResumenInventarioCategoriaDto
    {
        public int IdCategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public int? TotalMateriales { get; set; }
        public decimal? StockTotal { get; set; }
        public decimal? StockTotalFormateado { get; set; }
        public int? MaterialesBajoStock { get; set; }
        public double? PorcentajeBajoStock { get; set; }
        public int? MaterialesSinStock { get; set; }
    }

    /// <summary>
    /// DTO para el resumen de proyectos (desde VwProyectosResumen)
    /// </summary>
    public class DashboardProyectosResumenDto
    {
        public int IdProyecto { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string TipoProyecto { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
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
        public string Responsable { get; set; } = string.Empty;
        public int? DiasTranscurridos { get; set; }
        public string EstadoEntrega { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para el resumen de compras (desde VwComprasDetallada)
    /// </summary>
    public class DashboardComprasResumenDto
    {
        public int IdCompra { get; set; }
        public string NumeroComprobante { get; set; } = string.Empty;
        public string TipoComprobante { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public string? Moneda { get; set; }
        public decimal? Subtotal { get; set; }
        public DateTime FechaCompra { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaRecepcion { get; set; }
        public decimal? CostoUnitarioPen { get; set; }
    }

    /// <summary>
    /// DTO para el resumen de maquinaria disponible (desde VwMaquinariaDisponible)
    /// </summary>
    public class DashboardMaquinariaResumenDto
    {
        public int IdMaquinaria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? ImagenUrl { get; set; }
        public string Estado { get; set; } = string.Empty;

        public string EstadoBadge => Estado.ToLower() switch
        {
            "disponible" => "success",
            "prestada" => "warning",
            "mantenimiento" => "info",
            "fuera de servicio" => "danger",
            _ => "secondary"
        };
    }

    /// <summary>
    /// DTO para el resumen de empleados con recursos pendientes (desde VwEmpleadosRecursosPendiente)
    /// </summary>
    public class DashboardEmpleadoRecursoDto
    {
        public int IdAsignacionRecursoEmpleado { get; set; }
        public string Empleado { get; set; } = string.Empty;
        public string? CodigoProyecto { get; set; }
        public string Material { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CantidadDevuelta { get; set; }
        public decimal CantidadConsumida { get; set; }
        public decimal CantidadDanada { get; set; }
        public decimal? PendienteDevolucion { get; set; }
        public DateTime FechaEntrega { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para el resumen de contratistas con recursos pendientes (desde VwContratistasRecursosPendiente)
    /// </summary>
    public class DashboardContratistaRecursoDto
    {
        public int IdAsignacionRecurso { get; set; }
        public string Contratista { get; set; } = string.Empty;
        public string CodigoProyecto { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CantidadDevuelta { get; set; }
        public decimal CantidadConsumida { get; set; }
        public decimal CantidadDanada { get; set; }
        public decimal? PendienteDevolucion { get; set; }
        public DateTime FechaEntrega { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    // Indicadores financieros consolidados del Dashboard BI.
    public sealed class ResumenComprasBiDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public decimal TotalPEN { get; set; }
        public decimal TotalUSD { get; set; }
        public decimal TotalUSDenPEN { get; set; }
        public decimal? TotalConsolidadoPEN { get; set; }
        public int CantidadComprobantes { get; set; }
        public int ComprobantesSinTipoCambio { get; set; }
        public int MonedasNoReconocidas { get; set; }
        public List<string> FechasSinTipoCambio { get; set; } = new();
    }
}

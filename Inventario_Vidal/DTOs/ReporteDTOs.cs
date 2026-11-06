namespace Inventario_Vidal.DTOs
{
    // ================================================================
    // REPORTE - FILTRO BASE
    // ================================================================

    public class ReporteFiltroDto
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? IdProyecto { get; set; }
        public int? IdMaterial { get; set; }
        public int? IdCliente { get; set; }
        public int? IdProveedor { get; set; }
        public string? Estado { get; set; }
        public string? Tipo { get; set; }
        public string? Formato { get; set; } = "PDF";
    }

    // ================================================================
    // REPORTE - FILTROS AVANZADOS
    // ================================================================

    public class ReporteFiltrosAvanzadosDto : ReporteFiltroDto
    {
        public string? Search { get; set; }
        public string? Orden { get; set; }
        public string? Resultado { get; set; } // "ganancia" | "perdida"
        public string? Especialidad { get; set; }
        public int? IdCategoria { get; set; }
        public bool? SoloAlertas { get; set; }
        public int? IdContratista { get; set; }
        public int? IdEmpleado { get; set; }
        public string? Moneda { get; set; }
        public decimal? MontoMinimo { get; set; }
        public decimal? MontoMaximo { get; set; }
    }

    // ================================================================
    // REPORTE - STOCK (desde Materiales y VwAlertasStockBajo)
    // ================================================================

    public class ReporteStockDto
    {
        public int IdMaterial { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string UnidadConsumo { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal? StockMaximo { get; set; }
        public decimal StockReservado { get; set; }
        public decimal? StockDisponible { get; set; }
        public decimal? Valorizado { get; set; }
        public bool AlertaStockBajo { get; set; }
        public bool Activo { get; set; }

        public string Estado => AlertaStockBajo ? "⚠️ Stock Bajo" : "✅ Normal";
        public string EstadoBadge => AlertaStockBajo ? "danger" : "success";
    }

    // ================================================================
    // REPORTE - PROYECTO (desde VwProyectosResumen)
    // ================================================================

    public class ReporteProyectoDto
    {
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string TipoProyecto { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaEntregaEstimada { get; set; }
        public DateOnly? FechaEntregaReal { get; set; }
        public decimal? PrecioVenta { get; set; }
        public decimal CostoEstimado { get; set; }
        public decimal? CostoReal { get; set; }
        public decimal? UtilidadEstimada { get; set; }
        public decimal? UtilidadReal => (PrecioVenta ?? 0) - (CostoReal ?? 0);
        public string Moneda { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public int? DiasTranscurridos { get; set; }
        public string EstadoEntrega { get; set; } = string.Empty;

        public string EstadoBadge => ObtenerBadgeEstado(Estado);

        private static string ObtenerBadgeEstado(string estado)
        {
            return estado.ToLower() switch
            {
                "en proceso de fabricacion" => "primary",
                "en proceso de acabado" => "info",
                "instalacion de electricidad" => "warning",
                "terminado" => "success",
                "entregado" => "success",
                "pausado" => "secondary",
                "cancelado" => "danger",
                _ => "secondary"
            };
        }
    }

    // ================================================================
    // REPORTE - COMPRA (desde VwComprasDetallada)
    // ================================================================

    public class ReporteCompraDto
    {
        public int IdCompra { get; set; }
        public string NumeroComprobante { get; set; } = string.Empty;
        public string TipoComprobante { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public decimal? Subtotal { get; set; }
        public DateTime FechaCompra { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaRecepcion { get; set; }
        public decimal? CostoUnitarioPen { get; set; }

        public string FechaCompraFormateada => FechaCompra.ToString("dd/MM/yyyy");
        public string FechaRecepcionFormateada => FechaRecepcion?.ToString("dd/MM/yyyy") ?? "Pendiente";
        public string EstadoBadge => Estado.ToLower() switch
        {
            "registrada" => "success",
            "anulada" => "danger",
            "pendiente" => "warning",
            "recibida" => "info",
            _ => "secondary"
        };
        public decimal Total => Subtotal ?? (Cantidad * CostoUnitario);
    }

    // ================================================================
    // REPORTE - KARDEX (desde VwKardexDetallado)
    // ================================================================

    public class ReporteKardexDto
    {
        public long IdMovimiento { get; set; }
        public string CodigoBarras { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string TipoMovimiento { get; set; } = string.Empty;
        public short Signo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? StockAnterior { get; set; }
        public decimal? StockPosterior { get; set; }
        public string? CodigoProyecto { get; set; }
        public string? Contratista { get; set; }
        public string? Empleado { get; set; }
        public string? Comprobante { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;
        public DateTime FechaMovimiento { get; set; }
        public string? Observacion { get; set; }

        public string FechaFormateada => FechaMovimiento.ToString("dd/MM/yyyy HH:mm");
        public string TipoBadge => ObtenerBadge(TipoMovimiento);

        private static string ObtenerBadge(string tipo)
        {
            return tipo.ToLower() switch
            {
                "entrada por compra" => "success",
                "salida por asignacion a proyecto" => "warning",
                "devolucion de proyecto" => "info",
                "prestamo a contratista" => "warning",
                "devolucion de contratista" => "info",
                "entrega a empleado" => "warning",
                "devolucion de empleado" => "info",
                "ajuste positivo" => "success",
                "ajuste negativo" => "danger",
                "desperdicio/merma" => "danger",
                _ => "secondary"
            };
        }
    }

    // ================================================================
    // REPORTE - MAQUINARIA
    // ================================================================

    public class ReporteMaquinariaDto
    {
        public int IdMaquinaria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int TotalPrestamos { get; set; }
        public int TotalMantenimientos { get; set; }
        public decimal CostoMantenimiento { get; set; }
        public string Moneda { get; set; } = "PEN";

        public string EstadoBadge => Estado.ToLower() switch
        {
            "disponible" => "success",
            "prestada" => "warning",
            "mantenimiento" => "info",
            "fuera de servicio" => "danger",
            _ => "secondary"
        };
    }

    // ================================================================
    // REPORTE - CONSUMO POR PROYECTO (desde AsignacionMaterialesProyecto)
    // ================================================================

    public class ReporteConsumoProyectoDto
    {
        public int IdMaterial { get; set; }
        public string Material { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public string CategoriaMaterial { get; set; } = string.Empty;
        public string UnidadConsumo { get; set; } = string.Empty;
        public decimal CantidadAsignada { get; set; }
        public decimal CantidadUtilizada { get; set; }
        public decimal CantidadDevuelta { get; set; }
        public decimal CostoUnitarioRef { get; set; }
        public decimal TotalCosto => CantidadUtilizada * CostoUnitarioRef;
        public DateTime FechaAsignacion { get; set; }
        public int IdProyecto { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;

        public string FechaAsignacionFormateada => FechaAsignacion.ToString("dd/MM/yyyy HH:mm");
        public decimal PendienteDevolucion => CantidadAsignada - CantidadDevuelta - CantidadUtilizada;
        public decimal PorcentajeUtilizado => CantidadAsignada > 0 ? Math.Round((CantidadUtilizada / CantidadAsignada) * 100, 2) : 0;
    }

    // ================================================================
    // REPORTE - CLIENTE
    // ================================================================

    public class ReporteClienteDto
    {
        public int IdCliente { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreOrazonSocial { get; set; } = string.Empty;
        public string TipoCliente { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Direccion { get; set; }
        public string? Distrito { get; set; }
        public string? Provincia { get; set; }
        public string? Departamento { get; set; }
        public int TotalProyectos { get; set; }
        public decimal TotalInvertido { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }

        public string EstadoBadge => Activo ? "success" : "secondary";
        public string FechaRegistroFormateada => FechaRegistro.ToString("dd/MM/yyyy");
    }

    // ================================================================
    // REPORTE - CONTRATISTA
    // ================================================================

    public class ReporteContratistaDto
    {
        public int IdContratista { get; set; }
        public string NumeroDocumento { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }
        public int TotalProyectosAsignados { get; set; }
        public int TotalRecursosAsignados { get; set; }
        public int TotalHoras { get; set; }
        public decimal TarifaHora { get; set; }
        public decimal TotalPagado { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
        public int? Calificacion { get; set; }

        public string EstadoBadge => Activo ? "success" : "secondary";
        public string FechaRegistroFormateada => FechaRegistro.ToString("dd/MM/yyyy");
        public string CalificacionEstrellas => Calificacion.HasValue ? new string('★', Calificacion.Value) + new string('☆', 5 - Calificacion.Value) : "Sin calificar";
    }

    // ================================================================
    // REPORTE - UTILIDAD POR PROYECTO
    // ================================================================

    public class ReporteUtilidadDto
    {
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaEntregaReal { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Costos { get; set; }
        public decimal Utilidad { get; set; }
        public decimal Margen { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        public string FechaInicioFormateada => FechaInicio.ToString("dd/MM/yyyy");
        public string FechaEntregaRealFormateada => FechaEntregaReal?.ToString("dd/MM/yyyy") ?? "Pendiente";
        public string EstadoBadge => ObtenerBadgeEstado(Estado);

        private static string ObtenerBadgeEstado(string estado)
        {
            return estado.ToLower() switch
            {
                "en proceso de fabricacion" => "primary",
                "en proceso de acabado" => "info",
                "instalacion de electricidad" => "warning",
                "terminado" => "success",
                "entregado" => "success",
                "pausado" => "secondary",
                "cancelado" => "danger",
                _ => "secondary"
            };
        }
    }

    // ================================================================
    // REPORTE - COSTOS POR PROYECTO (desde VwCostosProyecto)
    // ================================================================

    public class ReporteCostosProyectoDto
    {
        public int IdCosto { get; set; }
        public int IdProyecto { get; set; }
        public string CodigoProyecto { get; set; } = string.Empty;
        public string TipoCosto { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;
        public decimal? MontoPen { get; set; }

        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        public string MontoFormateado => Monto.ToString("N2") + " " + Moneda;
    }

    // ================================================================
    // REPORTE - PRÉSTAMO DE MAQUINARIA
    // ================================================================

    public class ReportePrestamoMaquinariaDto
    {
        public int IdPrestamo { get; set; }
        public string Maquinaria { get; set; } = string.Empty;
        public string CodigoMaquinaria { get; set; } = string.Empty;
        public string PrestadoA { get; set; } = string.Empty;
        public string TipoPrestatario { get; set; } = string.Empty; // "Contratista" | "Empleado" | "Proyecto"
        public DateTime FechaPrestamo { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public DateTime? FechaDevolucionReal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? Proyecto { get; set; }
        public string? Observaciones { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;

        public bool EstaVencido => Estado == "Prestado" && FechaDevolucionEstimada.HasValue && FechaDevolucionEstimada.Value < DateOnly.FromDateTime(DateTime.Now);
        public int DiasPrestamo => FechaDevolucionReal.HasValue
            ? (int)(FechaDevolucionReal.Value - FechaPrestamo).TotalDays
            : (int)(DateTime.Now - FechaPrestamo).TotalDays;
        public string FechaPrestamoFormateada => FechaPrestamo.ToString("dd/MM/yyyy HH:mm");
        public string FechaDevolucionEstimadaFormateada => FechaDevolucionEstimada?.ToString("dd/MM/yyyy") ?? "No definida";
        public string FechaDevolucionRealFormateada => FechaDevolucionReal?.ToString("dd/MM/yyyy HH:mm") ?? "Pendiente";
        public string EstadoBadge => Estado.ToLower() switch
        {
            "prestado" => "warning",
            "devuelto" => "success",
            "atrasado" => "danger",
            _ => "secondary"
        };
    }

    // ================================================================
    // REPORTE - MOVIMIENTOS DE INVENTARIO (KARDEX RESUMIDO)
    // ================================================================

    public class ReporteMovimientosInventarioDto
    {
        public int IdMaterial { get; set; }
        public string Material { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public decimal StockInicial { get; set; }
        public decimal TotalEntradas { get; set; }
        public decimal TotalSalidas { get; set; }
        public decimal StockFinal { get; set; }
        public int CantidadMovimientos { get; set; }
        public DateTime? UltimoMovimiento { get; set; }

        public string UltimoMovimientoFormateada => UltimoMovimiento?.ToString("dd/MM/yyyy HH:mm") ?? "Sin movimientos";
        public string EstadoStock => StockFinal <= 0 ? "Sin Stock" : "Con Stock";
        public string EstadoBadge => StockFinal <= 0 ? "danger" : "success";
    }

    // ================================================================
    // ESTADÍSTICAS RÁPIDAS PARA DASHBOARD DE REPORTES
    // ================================================================

    public class EstadisticasRapidasDto
    {
        public int TotalMateriales { get; set; }
        public int TotalProyectos { get; set; }
        public int TotalCompras { get; set; }
        public int AlertasStock { get; set; }
        public int TotalClientes { get; set; }
        public int TotalContratistas { get; set; }
        public int TotalMaquinarias { get; set; }
        public decimal UtilidadTotal { get; set; }
        public decimal TotalComprasValor { get; set; }
        public int ProyectosActivos { get; set; }
        public int ProyectosEntregados { get; set; }
        public string Moneda { get; set; } = "PEN";
    }

    // ================================================================
    // REPORTE - MANTENIMIENTO DE MAQUINARIA (desde VwMantenimientoMaquinaria)
    // ================================================================

    public class ReporteMantenimientoMaquinariaDto
    {
        public int IdMantenimiento { get; set; }
        public int IdMaquinaria { get; set; }
        public string CodigoMaquinaria { get; set; } = string.Empty;
        public string Maquinaria { get; set; } = string.Empty;
        public string TipoMantenimiento { get; set; } = string.Empty;
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public string? Descripcion { get; set; }
        public decimal Costo { get; set; }
        public string? Moneda { get; set; }
        public string RegistradoPor { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public decimal? CostoPen { get; set; }

        public string FechaInicioFormateada => FechaInicio.ToString("dd/MM/yyyy");
        public string FechaFinFormateada => FechaFin?.ToString("dd/MM/yyyy") ?? "En curso";
        public string Estado => FechaFin.HasValue ? "Completado" : "En progreso";
        public string EstadoBadge => FechaFin.HasValue ? "success" : "warning";
        public int DiasDuracion => FechaFin.HasValue ? (FechaFin.Value.DayNumber - FechaInicio.DayNumber) : (DateOnly.FromDateTime(DateTime.Now).DayNumber - FechaInicio.DayNumber);
    }
    // ================================================================
    // AGREGAR AL FINAL DE ReporteDTOs.cs
    // ================================================================

    /// <summary>
    /// DTO para reporte de equipo (empleados y contratistas unificados)
    /// </summary>
    public class ReporteEquipoDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public string? Especialidad { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }
        public bool Activo { get; set; }
        public int ProyectosAsignados { get; set; }
        public int MaterialesAsignados { get; set; }
        public decimal TotalCantidad { get; set; }
        public decimal TotalUtilizado { get; set; }
        public decimal TotalDevuelto { get; set; }
        public decimal TotalPendiente { get; set; }
        public string ProyectoActual { get; set; } = string.Empty;

        public string EstadoBadge => Activo ? "success" : "secondary";
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    }
}
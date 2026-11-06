using System.ComponentModel.DataAnnotations;

namespace Inventario_Vidal.DTOs
{
    /// <summary>
    /// DTO para el resumen de alertas
    /// </summary>
    public class AlertaResumenDto
    {
        public int StockBajo { get; set; }
        public int DevolucionesContratistas { get; set; }
        public int DevolucionesEmpleados { get; set; }
        public int PrestamosVencidos { get; set; }
        public int TotalAlertas { get; set; }
    }

    /// <summary>
    /// DTO para una alerta individual en el sistema
    /// </summary>
    public class AlertaDto
    {
        public int IdAlerta { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public int? IdReferencia { get; set; }
        public string? Enlace { get; set; }
        public string? Icono { get; set; }
        public string? Color { get; set; }
        public bool Leida { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaLectura { get; set; }

        public string FechaFormateada => FechaCreacion.ToString("dd/MM/yyyy HH:mm");
        public string TiempoTranscurrido => ObtenerTiempoTranscurrido();

        private string ObtenerTiempoTranscurrido()
        {
            var diff = DateTime.Now - FechaCreacion;

            if (diff.TotalMinutes < 1)
                return "Hace unos segundos";
            if (diff.TotalMinutes < 60)
                return $"Hace {(int)diff.TotalMinutes} minuto(s)";
            if (diff.TotalHours < 24)
                return $"Hace {(int)diff.TotalHours} hora(s)";
            if (diff.TotalDays < 30)
                return $"Hace {(int)diff.TotalDays} día(s)";
            if (diff.TotalDays < 365)
                return $"Hace {(int)(diff.TotalDays / 30)} mes(es)";

            return $"Hace {(int)(diff.TotalDays / 365)} año(s)";
        }

        public string BadgeColor => Tipo.ToLower() switch
        {
            "stock_bajo" or "stock bajo" => "danger",
            "devolucion_contratista" or "devolucion contratista" => "warning",
            "devolucion_empleado" or "devolucion empleado" => "warning",
            "prestamo_vencido" or "prestamo vencido" => "danger",
            "proyecto_proximo" or "proyecto proximo" => "info",
            "proyecto_atrasado" or "proyecto atrasado" => "danger",
            _ => "secondary"
        };

        public string IconoAlerta => Tipo.ToLower() switch
        {
            "stock_bajo" or "stock bajo" => "alert-triangle",
            "devolucion_contratista" or "devolucion contratista" => "hardhat",
            "devolucion_empleado" or "devolucion empleado" => "user",
            "prestamo_vencido" or "prestamo vencido" => "tool",
            "proyecto_proximo" or "proyecto proximo" => "clock",
            "proyecto_atrasado" or "proyecto atrasado" => "alert-circle",
            _ => "bell"
        };
    }

    /// <summary>
    /// DTO para el conteo de alertas por tipo
    /// </summary>
    public class AlertaConteoDto
    {
        public int Total { get; set; }
        public int NoLeidas { get; set; }
        public Dictionary<string, int> PorTipo { get; set; } = new();
    }

    // ================================================================
    // ALERTA DE STOCK BAJO (desde VwAlertasStockBajo)
    // ✅ CORREGIDO: AlertaStockBajo es int en la vista
    // ================================================================

    public class AlertaStockBajoDto
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
        public string? ImagenUrl { get; set; }
        public string EstadoStock { get; set; } = string.Empty;
        public int AlertaStockBajo { get; set; }  // ✅ int (0 o 1)
        public bool Activo { get; set; }

        public bool TieneAlerta => AlertaStockBajo == 1;
    }

    // ================================================================
    // ALERTA DE DEVOLUCIÓN DE CONTRATISTA (desde AsignacionMaterialesPersonal)
    // ✅ CORREGIDO: usa la nueva tabla
    // ================================================================

    public class AlertaDevolucionContratistaDto
    {
        public int IdAsignacion { get; set; }
        public string Contratista { get; set; } = string.Empty;
        public string CodigoProyecto { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public decimal CantidadEntregada { get; set; }
        public decimal CantidadDevuelta { get; set; }
        public decimal CantidadConsumida { get; set; }
        public decimal CantidadDanada { get; set; }
        public decimal? PendienteDevolucion { get; set; }
        public DateTime FechaAsignacion { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    // ================================================================
    // ALERTA DE DEVOLUCIÓN DE EMPLEADO (desde AsignacionMaterialesPersonal)
    // ✅ CORREGIDO: usa la nueva tabla
    // ================================================================

    public class AlertaDevolucionEmpleadoDto
    {
        public int IdAsignacion { get; set; }
        public string Empleado { get; set; } = string.Empty;
        public string? CodigoProyecto { get; set; }
        public string Material { get; set; } = string.Empty;
        public decimal CantidadEntregada { get; set; }
        public decimal CantidadDevuelta { get; set; }
        public decimal CantidadConsumida { get; set; }
        public decimal CantidadDanada { get; set; }
        public decimal? PendienteDevolucion { get; set; }
        public DateTime FechaAsignacion { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    // ================================================================
    // ALERTA DE PRÉSTAMO DE MAQUINARIA VENCIDO
    // ================================================================

    public class AlertaPrestamoMaquinariaVencidoDto
    {
        public int IdPrestamo { get; set; }
        public int IdMaquinaria { get; set; }
        public string CodigoMaquinaria { get; set; } = string.Empty;
        public string MaquinariaNombre { get; set; } = string.Empty;
        public DateTime FechaPrestamo { get; set; }
        public DateOnly? FechaDevolucionEstimada { get; set; }
        public int DiasVencidos { get; set; }
        public string Destinatario { get; set; } = string.Empty;
        public string TipoDestinatario { get; set; } = string.Empty;
    }

    // ================================================================
    // FILTRO DE ALERTAS
    // ================================================================

    public class AlertaFiltroDto
    {
        public string? Tipo { get; set; }
        public string? Nivel { get; set; }
        public bool? SoloNoLeidas { get; set; }
        public int? IdProyecto { get; set; }
        public int? IdMaterial { get; set; }
        public int? IdMaquinaria { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    // ================================================================
    // MARCAR ALERTA COMO LEÍDA
    // ================================================================

    public class MarcarAlertaLeidaDto
    {
        [Required(ErrorMessage = "El ID de la alerta es obligatorio")]
        public int IdAlerta { get; set; }
    }

    // ================================================================
    // MARCAR TODAS LAS ALERTAS COMO LEÍDAS
    // ================================================================

    public class MarcarTodasAlertasLeidasDto
    {
        public string? Tipo { get; set; }
    }

    // ================================================================
    // ALERTAS STOCK (para el dashboard)
    // ✅ CORREGIDO: AlertaStockBajo es int
    // ================================================================

    public class AlertasStockDto
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
        public string? ImagenUrl { get; set; }
        public string EstadoStock { get; set; } = string.Empty;
        public int AlertaStockBajo { get; set; }  // ✅ int (0 o 1)
        public bool Activo { get; set; }

        public bool TieneAlerta => AlertaStockBajo == 1;
        public int PorcentajeAlerta => StockMinimo > 0 ? (int)Math.Round((StockActual / StockMinimo) * 100) : 0;
        public string NivelAlerta => PorcentajeAlerta > 50 ? "danger" : "warning";
    }
}
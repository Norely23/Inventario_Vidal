using System.ComponentModel.DataAnnotations;

namespace Inventario_Vidal.DTOs
{
    /// <summary>
    /// DTO para los datos de la empresa (desde ConfiguracionEmpresa)
    /// </summary>
    public class EmpresaDto
    {
        public int IdConfiguracion { get; set; }

        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres")]
        [Display(Name = "Nombre de la Empresa")]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUC es obligatorio")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "El RUC debe tener entre 8 y 20 caracteres")]
        [Display(Name = "RUC")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El RUC solo debe contener números")]
        public string Ruc { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La dirección no debe exceder los 250 caracteres")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(30, ErrorMessage = "El teléfono no debe exceder los 30 caracteres")]
        [Display(Name = "Teléfono")]
        [RegularExpression(@"^[0-9+\- ]+$", ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [StringLength(150, ErrorMessage = "El correo no debe exceder los 150 caracteres")]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }

        [StringLength(150, ErrorMessage = "El sitio web no debe exceder los 150 caracteres")]
        [Display(Name = "Sitio Web")]
        [Url(ErrorMessage = "El formato de URL no es válido")]
        public string? SitioWeb { get; set; }

        [StringLength(500, ErrorMessage = "La URL del logo no debe exceder los 500 caracteres")]
        [Display(Name = "Logo")]
        public string? LogoUrl { get; set; }

        [StringLength(500, ErrorMessage = "El pie de página no debe exceder los 500 caracteres")]
        [Display(Name = "Pie de Página")]
        public string? PiePagina { get; set; }

        [Required(ErrorMessage = "La moneda principal es obligatoria")]
        [RegularExpression(@"^(PEN|USD|EUR)$", ErrorMessage = "Seleccione una moneda principal válida")]
        [Display(Name = "Moneda Principal")]
        public string Moneda { get; set; } = "PEN";

        [RegularExpression(@"^(PEN|USD|EUR)$", ErrorMessage = "Seleccione una moneda secundaria válida")]
        [Display(Name = "Moneda Secundaria")]
        public string? MonedaSecundaria { get; set; }

        [Display(Name = "Tipo de Cambio")]
        [Range(0.01, 9999.99, ErrorMessage = "El tipo de cambio debe ser un valor positivo")]
        public decimal? TipoCambio { get; set; }

        [Required(ErrorMessage = "El IGV es obligatorio")]
        [Display(Name = "IGV (%)")]
        [Range(0, 100, ErrorMessage = "El IGV debe estar entre 0 y 100")]
        public decimal Igv { get; set; }
    }

    /// <summary>
    /// DTO para crear configuración de empresa
    /// </summary>
    public class EmpresaCreateDto
    {
        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres")]
        [Display(Name = "Nombre de la Empresa")]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUC es obligatorio")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "El RUC debe tener entre 8 y 20 caracteres")]
        [Display(Name = "RUC")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El RUC solo debe contener números")]
        public string Ruc { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La dirección no debe exceder los 250 caracteres")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(30, ErrorMessage = "El teléfono no debe exceder los 30 caracteres")]
        [Display(Name = "Teléfono")]
        [RegularExpression(@"^[0-9+\- ]+$", ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [StringLength(150, ErrorMessage = "El correo no debe exceder los 150 caracteres")]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }

        [StringLength(150, ErrorMessage = "El sitio web no debe exceder los 150 caracteres")]
        [Display(Name = "Sitio Web")]
        [Url(ErrorMessage = "El formato de URL no es válido")]
        public string? SitioWeb { get; set; }

        [StringLength(500, ErrorMessage = "El pie de página no debe exceder los 500 caracteres")]
        [Display(Name = "Pie de Página")]
        public string? PiePagina { get; set; }

        [Required(ErrorMessage = "La moneda principal es obligatoria")]
        [RegularExpression(@"^(PEN|USD|EUR)$", ErrorMessage = "Seleccione una moneda principal válida")]
        [Display(Name = "Moneda Principal")]
        public string Moneda { get; set; } = "PEN";

        [RegularExpression(@"^(PEN|USD|EUR)$", ErrorMessage = "Seleccione una moneda secundaria válida")]
        [Display(Name = "Moneda Secundaria")]
        public string? MonedaSecundaria { get; set; }

        [Display(Name = "Tipo de Cambio")]
        [Range(0.01, 9999.99, ErrorMessage = "El tipo de cambio debe ser un valor positivo")]
        public decimal? TipoCambio { get; set; }

        [Required(ErrorMessage = "El IGV es obligatorio")]
        [Display(Name = "IGV (%)")]
        [Range(0, 100, ErrorMessage = "El IGV debe estar entre 0 y 100")]
        public decimal Igv { get; set; }

        [Display(Name = "Logo")]
        public IFormFile? LogoArchivo { get; set; }
    }

    /// <summary>
    /// DTO para actualizar configuración de empresa
    /// </summary>
    public class EmpresaUpdateDto
    {
        public int IdConfiguracion { get; set; }

        [Required(ErrorMessage = "El nombre de la empresa es obligatorio")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 200 caracteres")]
        [Display(Name = "Nombre de la Empresa")]
        public string NombreEmpresa { get; set; } = string.Empty;

        [Required(ErrorMessage = "El RUC es obligatorio")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "El RUC debe tener entre 8 y 20 caracteres")]
        [Display(Name = "RUC")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El RUC solo debe contener números")]
        public string Ruc { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "La dirección no debe exceder los 250 caracteres")]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(30, ErrorMessage = "El teléfono no debe exceder los 30 caracteres")]
        [Display(Name = "Teléfono")]
        [RegularExpression(@"^[0-9+\- ]+$", ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [StringLength(150, ErrorMessage = "El correo no debe exceder los 150 caracteres")]
        [Display(Name = "Correo Electrónico")]
        public string? CorreoElectronico { get; set; }

        [StringLength(150, ErrorMessage = "El sitio web no debe exceder los 150 caracteres")]
        [Display(Name = "Sitio Web")]
        [Url(ErrorMessage = "El formato de URL no es válido")]
        public string? SitioWeb { get; set; }

        [StringLength(500, ErrorMessage = "El pie de página no debe exceder los 500 caracteres")]
        [Display(Name = "Pie de Página")]
        public string? PiePagina { get; set; }

        [Required(ErrorMessage = "La moneda principal es obligatoria")]
        [RegularExpression(@"^(PEN|USD|EUR)$", ErrorMessage = "Seleccione una moneda principal válida")]
        [Display(Name = "Moneda Principal")]
        public string Moneda { get; set; } = "PEN";

        [RegularExpression(@"^(PEN|USD|EUR)$", ErrorMessage = "Seleccione una moneda secundaria válida")]
        [Display(Name = "Moneda Secundaria")]
        public string? MonedaSecundaria { get; set; }

        [Display(Name = "Tipo de Cambio")]
        [Range(0.01, 9999.99, ErrorMessage = "El tipo de cambio debe ser un valor positivo")]
        public decimal? TipoCambio { get; set; }

        [Required(ErrorMessage = "El IGV es obligatorio")]
        [Display(Name = "IGV (%)")]
        [Range(0, 100, ErrorMessage = "El IGV debe estar entre 0 y 100")]
        public decimal Igv { get; set; }

        [Display(Name = "Logo")]
        public IFormFile? LogoArchivo { get; set; }

        [Display(Name = "Eliminar Logo Actual")]
        public bool EliminarLogo { get; set; }
    }

    /// <summary>
    /// DTO para la configuración de logo
    /// </summary>
    public class LogoDto
    {
        public string? LogoUrl { get; set; }
        public string? LogoBase64 { get; set; }
        public string? NombreArchivo { get; set; }
        public string? Extension { get; set; }
        public long? Tamanio { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    /// <summary>
    /// DTO para el tipo de cambio (desde TiposCambio)
    /// </summary>
    public class TipoCambioDto
    {
        public int IdTipoCambio { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha")]
        public DateOnly Fecha { get; set; }

        [Required(ErrorMessage = "La moneda de origen es obligatoria")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "La moneda debe tener entre 3 y 10 caracteres")]
        [Display(Name = "Moneda Origen")]
        public string MonedaOrigen { get; set; } = string.Empty;

        [Required(ErrorMessage = "La moneda de destino es obligatoria")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "La moneda debe tener entre 3 y 10 caracteres")]
        [Display(Name = "Moneda Destino")]
        public string MonedaDestino { get; set; } = string.Empty;

        [Required(ErrorMessage = "El valor es obligatorio")]
        [Display(Name = "Valor")]
        [Range(0.0001, 99999.9999, ErrorMessage = "El valor debe ser positivo")]
        public decimal Valor { get; set; }

        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public DateTime FechaRegistro { get; set; }

        public string FechaFormateada => Fecha.ToString("dd/MM/yyyy");
        public string FechaRegistroFormateada => FechaRegistro.ToString("dd/MM/yyyy HH:mm");
    }

    /// <summary>
    /// DTO para crear tipo de cambio
    /// </summary>
    public class TipoCambioCreateDto
    {
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [Display(Name = "Fecha")]
        public DateOnly Fecha { get; set; }

        [Required(ErrorMessage = "La moneda de origen es obligatoria")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "La moneda debe tener entre 3 y 10 caracteres")]
        [Display(Name = "Moneda Origen")]
        public string MonedaOrigen { get; set; } = "USD";

        [Required(ErrorMessage = "La moneda de destino es obligatoria")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "La moneda debe tener entre 3 y 10 caracteres")]
        [Display(Name = "Moneda Destino")]
        public string MonedaDestino { get; set; } = "PEN";

        [Required(ErrorMessage = "El valor es obligatorio")]
        [Display(Name = "Valor")]
        [Range(0.0001, 99999.9999, ErrorMessage = "El valor debe ser positivo")]
        public decimal Valor { get; set; }
    }

    /// <summary>
    /// DTO para filtro de tipos de cambio
    /// </summary>
    public class TipoCambioFiltroDto
    {
        public string? MonedaOrigen { get; set; }
        public string? MonedaDestino { get; set; }
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public int? UsuarioId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// DTO para vista previa de empresa
    /// </summary>
    public class EmpresaVistaDto
    {
        public string NombreEmpresa { get; set; } = string.Empty;
        public string? Ruc { get; set; }
        public string? LogoUrl { get; set; }
        public string? PiePagina { get; set; }
        public string Moneda { get; set; } = "PEN";
        public decimal Igv { get; set; }
        public decimal? TipoCambio { get; set; }
    }

    /// <summary>
    /// DTO para resumen de configuración de empresa
    /// </summary>
    public class EmpresaResumenDto
    {
        public string NombreEmpresa { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string Moneda { get; set; } = "PEN";
        public decimal Igv { get; set; }
        public decimal? TipoCambio { get; set; }
        public DateTime? UltimaActualizacion { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalProyectos { get; set; }
        public int TotalMateriales { get; set; }
    }

    /// <summary>
    /// DTO para la configuración de reportes de empresa (desde VwConfiguracionReporte)
    /// </summary>
    public class EmpresaConfiguracionReporteDto
    {
        public string NombreEmpresa { get; set; } = string.Empty;
        public string Ruc { get; set; } = string.Empty;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? SitioWeb { get; set; }
        public string? LogoUrl { get; set; }
        public string? PiePagina { get; set; }
        public string Moneda { get; set; } = string.Empty;
        public string? MonedaSecundaria { get; set; }
        public decimal? TipoCambio { get; set; }
        public decimal Igv { get; set; }

        public int? ProyectosActivos { get; set; }
        public int? ProyectosEntregados { get; set; }
        public int? AlertasStockBajo { get; set; }
        public int? MaterialesSinStock { get; set; }
    }
}
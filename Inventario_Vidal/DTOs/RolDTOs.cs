using System.ComponentModel.DataAnnotations;

namespace Inventario_Vidal.DTOs
{
    // ================================================================
    // ROL - LISTADO (desde Roles)
    // ================================================================

    /// <summary>
    /// DTO para listar roles
    /// </summary>
    public class RolListDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public int CantidadUsuarios { get; set; }
        public DateTime FechaCreacion { get; set; }

        public string EstadoBadge => Activo ? "success" : "secondary";
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
        public string FechaCreacionFormateada => FechaCreacion.ToString("dd/MM/yyyy HH:mm");
        public string Iniciales => ObtenerIniciales(NombreRol);

        private static string ObtenerIniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "R";
            var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0].Substring(0, 1).ToUpper();
            return (partes[0].Substring(0, 1) + partes[^1].Substring(0, 1)).ToUpper();
        }
    }

    // ================================================================
    // ROL - CREAR
    // ================================================================

    /// <summary>
    /// DTO para crear un rol
    /// </summary>
    public class RolCreateDto
    {
        [Required(ErrorMessage = "El nombre del rol es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 50 caracteres")]
        [Display(Name = "Nombre del Rol")]
        [RegularExpression(@"^[a-zA-ZáéíóúñÑ ]+$", ErrorMessage = "Solo se permiten letras y espacios")]
        public string NombreRol { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "La descripción no debe exceder los 200 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;
    }

    // ================================================================
    // ROL - EDITAR
    // ================================================================

    /// <summary>
    /// DTO para editar un rol
    /// </summary>
    public class RolEditDto
    {
        public int IdRol { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 50 caracteres")]
        [Display(Name = "Nombre del Rol")]
        [RegularExpression(@"^[a-zA-ZáéíóúñÑ ]+$", ErrorMessage = "Solo se permiten letras y espacios")]
        public string NombreRol { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "La descripción no debe exceder los 200 caracteres")]
        [Display(Name = "Descripción")]
        public string? Descripcion { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }
    }

    // ================================================================
    // ROL - DETALLE
    // ================================================================

    /// <summary>
    /// DTO para detalle de un rol
    /// </summary>
    public class RolDetailDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public int CantidadUsuarios { get; set; }
        public List<UsuarioRolDetailDto> Usuarios { get; set; } = new();

        public string EstadoBadge => Activo ? "success" : "secondary";
        public string EstadoTexto => Activo ? "Activo" : "Inactivo";
        public string FechaCreacionFormateada => FechaCreacion.ToString("dd/MM/yyyy HH:mm");
        public string Iniciales => ObtenerIniciales(NombreRol);

        private static string ObtenerIniciales(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre)) return "R";
            var partes = nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0].Substring(0, 1).ToUpper();
            return (partes[0].Substring(0, 1) + partes[^1].Substring(0, 1)).ToUpper();
        }
    }

    // ================================================================
    // ROL - USUARIO DETALLE
    // ================================================================

    public class UsuarioRolDetailDto
    {
        public int IdUsuario { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string? CorreoElectronico { get; set; }
        public bool Activo { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        public string EstadoBadge => Activo ? "success" : "secondary";
        public string UltimoAccesoFormateado => UltimoAcceso?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";
    }

    // ================================================================
    // ROL - SELECT (PARA DROPDOWN)
    // ================================================================

    /// <summary>
    /// DTO para selector de roles
    /// </summary>
    public class RolSelectDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public bool Activo { get; set; }

        public string DisplayText => NombreRol;
    }

    // ================================================================
    // ROL - ESTADÍSTICAS
    // ================================================================

    public class RolEstadisticasDto
    {
        public int TotalRoles { get; set; }
        public int RolesActivos { get; set; }
        public int RolesInactivos { get; set; }
        public int TotalUsuarios { get; set; }
        public Dictionary<string, int> UsuariosPorRol { get; set; } = new();
        public DateTime? UltimoRolCreado { get; set; }
    }

    // ================================================================
    // ROL - PERMISOS (para asignación de permisos)
    // ================================================================

    /// <summary>
    /// DTO para asignar permisos a un rol
    /// </summary>
    public class RolPermisosDto
    {
        public int IdRol { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public List<PermisoDto> Permisos { get; set; } = new();
    }

    /// <summary>
    /// DTO para un permiso individual
    /// </summary>
    public class PermisoDto
    {
        public int IdPermiso { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Modulo { get; set; } = string.Empty;
        public string Icono { get; set; } = string.Empty;
        public bool Asignado { get; set; }
        public string? Categoria { get; set; }
    }

    // ================================================================
    // ROL - ASIGNAR PERMISO
    // ================================================================

    public class RolAsignarPermisoDto
    {
        [Required]
        public int IdRol { get; set; }

        [Required]
        public int IdPermiso { get; set; }

        public bool Asignado { get; set; }
    }

    // ================================================================
    // ROL - ASIGNAR MÚLTIPLES PERMISOS
    // ================================================================

    public class RolAsignarMultiplesPermisosDto
    {
        [Required]
        public int IdRol { get; set; }

        [Required]
        public List<int> IdsPermisos { get; set; } = new();
    }
}
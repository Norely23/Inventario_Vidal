using System.ComponentModel.DataAnnotations;

namespace Inventario_Vidal.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordar sesión")]
        public bool RecordarSesion { get; set; }

        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class AuthResultDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? IdUsuario { get; set; }
        public string? NombreCompleto { get; set; }
        public string? NombreUsuario { get; set; }
        public string? CorreoElectronico { get; set; }
        public int? IdRol { get; set; }
        public string? NombreRol { get; set; }
        public string? Token { get; set; }
        public bool RecordarSesion { get; set; }
        public DateTime? UltimoAcceso { get; set; }

        public static AuthResultDto Error(string message)
        {
            return new AuthResultDto { Success = false, Message = message };
        }

        public static AuthResultDto Ok(
            int idUsuario,
            string nombreCompleto,
            string nombreUsuario,
            string? correoElectronico,
            int idRol,
            string nombreRol,
            string token,
            bool recordarSesion,
            DateTime? ultimoAcceso)
        {
            return new AuthResultDto
            {
                Success = true,
                Message = "Login exitoso",
                IdUsuario = idUsuario,
                NombreCompleto = nombreCompleto,
                NombreUsuario = nombreUsuario,
                CorreoElectronico = correoElectronico,
                IdRol = idRol,
                NombreRol = nombreRol,
                Token = token,
                RecordarSesion = recordarSesion,
                UltimoAcceso = ultimoAcceso
            };
        }
    }

    public class PerfilUsuarioDto
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(150, ErrorMessage = "El nombre completo no puede superar 150 caracteres")]
        [Display(Name = "Nombre completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido")]
        [StringLength(150, ErrorMessage = "El correo no puede superar 150 caracteres")]
        [Display(Name = "Correo electrónico")]
        public string? CorreoElectronico { get; set; }

        public string NombreRol { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }

    public class CambiarPasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string PasswordActual { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        [Display(Name = "Nueva contraseña")]
        public string PasswordNueva { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirma la nueva contraseña")]
        [DataType(DataType.Password)]
        [Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }

    public class SesionUsuarioDto
    {
        public long TokenId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaExpiracion { get; set; }
        public bool Activa { get; set; }
        public bool EsSesionActual { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public string? DispositivoRegistrado { get; set; }

        public string FechaFormateada => FechaCreacion.ToString("dd/MM/yyyy HH:mm");
        public string FechaExpiracionFormateada => FechaExpiracion.ToString("dd/MM/yyyy HH:mm");
        public string Estado => Activa ? "Activa" : "Cerrada";
        public string Dispositivo => ObtenerDispositivo(UserAgent, DispositivoRegistrado);
        public string Navegador => ObtenerNavegador(UserAgent);

        private static string ObtenerDispositivo(string? userAgent, string? dispositivoRegistrado)
        {
            if (!string.IsNullOrWhiteSpace(dispositivoRegistrado))
                return dispositivoRegistrado;

            if (string.IsNullOrWhiteSpace(userAgent))
                return "Desconocido";

            if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Tablet", StringComparison.OrdinalIgnoreCase))
                return "Tablet";

            if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                return "Móvil";

            return "Escritorio";
        }

        private static string ObtenerNavegador(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
                return "Desconocido";

            if (userAgent.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
                return "Microsoft Edge";

            if (userAgent.Contains("OPR/", StringComparison.OrdinalIgnoreCase) ||
                userAgent.Contains("Opera", StringComparison.OrdinalIgnoreCase))
                return "Opera";

            if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                return "Firefox";

            if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                return "Chrome";

            if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
                return "Safari";

            return "Desconocido";
        }
    }

    public class ServiceResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public static ServiceResultDto Ok(string message)
        {
            return new ServiceResultDto { Success = true, Message = message };
        }

        public static ServiceResultDto Error(string message)
        {
            return new ServiceResultDto { Success = false, Message = message };
        }
    }
}

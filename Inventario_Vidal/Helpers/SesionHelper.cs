namespace Inventario_Vidal.Helpers
{
    public static class SesionHelper
    {
        private const string USUARIO_ID = "IdUsuario";
        private const string USERNAME = "NombreUsuario";
        private const string ROL = "NombreRol";
        private const string NOMBRE_COMPLETO = "NombreCompleto";
        private const string TOKEN_SESION = "TokenSesion";
        private const string ULTIMO_ACCESO = "UltimoAcceso";

        public static bool TieneSesion(IHttpContextAccessor accessor)
        {
            return accessor.HttpContext?.Session.GetInt32(USUARIO_ID).HasValue ?? false;
        }

        public static int? GetUsuarioId(IHttpContextAccessor accessor)
        {
            return accessor.HttpContext?.Session.GetInt32(USUARIO_ID);
        }

        public static string? GetUsername(IHttpContextAccessor accessor)
        {
            return accessor.HttpContext?.Session.GetString(USERNAME);
        }

        public static string? GetRol(IHttpContextAccessor accessor)
        {
            return accessor.HttpContext?.Session.GetString(ROL);
        }

        public static string? GetNombreCompleto(IHttpContextAccessor accessor)
        {
            return accessor.HttpContext?.Session.GetString(NOMBRE_COMPLETO);
        }

        public static string? GetTokenSesion(IHttpContextAccessor accessor)
        {
            return accessor.HttpContext?.Session.GetString(TOKEN_SESION);
        }

        public static DateTime? GetUltimoAcceso(IHttpContextAccessor accessor)
        {
            var valor = accessor.HttpContext?.Session.GetString(ULTIMO_ACCESO);
            return valor != null ? DateTime.Parse(valor) : null;
        }

        public static void IniciarSesion(
            IHttpContextAccessor accessor,
            int usuarioId,
            string username,
            string rol,
            string nombreCompleto,
            string token)
        {
            var session = accessor.HttpContext?.Session;
            if (session == null) return;

            session.SetInt32(USUARIO_ID, usuarioId);
            session.SetString(USERNAME, username);
            session.SetString(ROL, rol);
            session.SetString(NOMBRE_COMPLETO, nombreCompleto);
            session.SetString(TOKEN_SESION, token);
            session.SetString(ULTIMO_ACCESO, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        public static void CerrarSesion(IHttpContextAccessor accessor)
        {
            accessor.HttpContext?.Session.Clear();
        }

        public static bool EsRol(IHttpContextAccessor accessor, string rol)
        {
            var rolActual = GetRol(accessor);
            return !string.IsNullOrEmpty(rolActual) && rolActual.Equals(rol, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EsRol(IHttpContextAccessor accessor, params string[] roles)
        {
            var rolActual = GetRol(accessor);
            if (string.IsNullOrEmpty(rolActual)) return false;
            return roles.Any(r => r.Equals(rolActual, StringComparison.OrdinalIgnoreCase));
        }

        public static object GetUsuarioActual(IHttpContextAccessor accessor)
        {
            return new
            {
                UsuarioId = GetUsuarioId(accessor),
                Username = GetUsername(accessor),
                Rol = GetRol(accessor),
                NombreCompleto = GetNombreCompleto(accessor)
            };
        }

        public static void RefreshUltimoAcceso(IHttpContextAccessor accessor)
        {
            accessor.HttpContext?.Session.SetString(ULTIMO_ACCESO, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
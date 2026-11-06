// Helpers/PasswordHelper.cs
using System.Security.Cryptography;
using System.Text;

namespace Inventario_Vidal.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        public static string GenerarPasswordAleatoria(int longitud = 10)
        {
            const string caracteres = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";
            var bytes = new byte[longitud];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);

            var result = new StringBuilder(longitud);
            foreach (var b in bytes)
            {
                result.Append(caracteres[b % caracteres.Length]);
            }

            return result.ToString();
        }
    }
}
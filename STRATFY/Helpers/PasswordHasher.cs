// STRATFY.Helpers/PasswordHasher.cs
using System;
using System.Security.Cryptography;
using System.Text;

namespace STRATFY.Helpers
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 10000; // Número de iterações para PBKDF2

        public static string HashPassword(string password)
        {
            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithmName.SHA256))
            {
                var salt = algorithm.Salt;
                var hash = algorithm.GetBytes(KeySize);

                // Concatena o salt e o hash para armazenamento
                var hashBytes = new byte[SaltSize + KeySize];
                Buffer.BlockCopy(salt, 0, hashBytes, 0, SaltSize);
                Buffer.BlockCopy(hash, 0, hashBytes, SaltSize, KeySize);

                return Convert.ToBase64String(hashBytes);
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashBytes = Convert.FromBase64String(hashedPassword);

            var salt = new byte[SaltSize];
            Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

            var storedHash = new byte[KeySize];
            Buffer.BlockCopy(hashBytes, SaltSize, storedHash, 0, KeySize);

            using (var algorithm = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                var newHash = algorithm.GetBytes(KeySize);
                // Compara os hashes byte a byte para evitar ataques de temporização
                return SlowEquals(storedHash, newHash);
            }
        }

        // Ajuda a mitigar ataques de temporização
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}
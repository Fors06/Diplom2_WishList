using System;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace WishList.Services
{
    public static class PasswordHasher
    {
        // Универсальная проверка пароля
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            // Если хеш в формате ASP.NET Identity (начинается с AQAAAA)
            if (IsAspNetIdentityHash(storedHash))
            {
                return VerifyAspNetIdentityPassword(password, storedHash);
            }
            // Если хеш в Base64 (SHA256)
            else if (IsBase64Hash(storedHash))
            {
                return VerifySha256Password(password, storedHash);
            }
            // Если MD5 (32 символа hex)
            else if (IsMd5Hash(storedHash))
            {
                return VerifyMd5Password(password, storedHash);
            }
            // Если plain text (последний вариант)
            else
            {
                return password == storedHash;
            }
        }



        // Проверка ASP.NET Identity пароля
        private static bool VerifyAspNetIdentityPassword(string password, string storedHash)
        {
            try
            {
                byte[] hashedPasswordBytes = Convert.FromBase64String(storedHash);

                // Проверяем версию формата (должна быть 0x01 для V3)
                if (hashedPasswordBytes[0] != 0x01)
                    return false;

                // Извлекаем параметры из хеша
                ReadOnlySpan<byte> hashedPasswordSpan = hashedPasswordBytes;

                // Пропускаем версию (1 байт)
                hashedPasswordSpan = hashedPasswordSpan.Slice(1);

                // Читаем флаги (1 байт)
                byte flags = hashedPasswordSpan[0];
                hashedPasswordSpan = hashedPasswordSpan.Slice(1);

                // Определяем алгоритм по флагам
                KeyDerivationPrf prf = (flags & 1) != 0 ? KeyDerivationPrf.HMACSHA256 : KeyDerivationPrf.HMACSHA512;

                // Читаем итерации (4 байта)
                uint iterCount = BitConverter.ToUInt32(hashedPasswordSpan.Slice(0, 4));
                hashedPasswordSpan = hashedPasswordSpan.Slice(4);

                // Читаем размер соли (4 байта)
                int saltLength = BitConverter.ToInt32(hashedPasswordSpan.Slice(0, 4));
                hashedPasswordSpan = hashedPasswordSpan.Slice(4);

                // Читаем соль
                byte[] salt = hashedPasswordSpan.Slice(0, saltLength).ToArray();
                hashedPasswordSpan = hashedPasswordSpan.Slice(saltLength);

                // Читаем хеш
                byte[] expectedHash = hashedPasswordSpan.ToArray();

                // Вычисляем хеш для введенного пароля
                byte[] actualHash = KeyDerivation.Pbkdf2(
                    password: password,
                    salt: salt,
                    prf: prf,
                    iterationCount: (int)iterCount,
                    numBytesRequested: expectedHash.Length);

                // Сравниваем хеши
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
            catch
            {
                return false;
            }
        }

        // Проверка SHA256 пароля
        private static bool VerifySha256Password(string password, string storedHash)
        {
            try
            {
                var hashedInput = HashPasswordSha256(password);
                return hashedInput == storedHash;
            }
            catch
            {
                return false;
            }
        }

        // Проверка MD5 пароля
        private static bool VerifyMd5Password(string password, string md5Hash)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = md5.ComputeHash(bytes);
                var computedHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
                return computedHash == md5Hash.ToLower();
            }
        }

        // SHA256 хеширование
        public static string HashPasswordSha256(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // MD5 хеширование
        public static string HashPasswordMd5(string password)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = md5.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        // Вспомогательные методы для определения типа хеша
        private static bool IsAspNetIdentityHash(string hash)
        {
            return hash.StartsWith("AQAAAA");
        }

        private static bool IsBase64Hash(string hash)
        {
            if (string.IsNullOrEmpty(hash) || hash.Length % 4 != 0)
                return false;

            try
            {
                Convert.FromBase64String(hash);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsMd5Hash(string hash)
        {
            return hash.Length == 32 && hash.All(c =>
                (c >= '0' && c <= '9') ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F'));
        }
    }
}
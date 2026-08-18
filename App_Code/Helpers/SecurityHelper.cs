using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CRMP.Helpers
{
    public static class SecurityHelper
    {
        // ── Password hashing (PBKDF2 / SHA-256) ──────────────────────────────
        private const int SaltSize    = 16;
        private const int HashSize    = 32;
        private const int Iterations  = 10000;

        public static string HashPassword(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);
                byte[] combined = new byte[SaltSize + HashSize];
                Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
                Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);
                return Convert.ToBase64String(combined);
            }
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                byte[] combined = Convert.FromBase64String(storedHash);
                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(HashSize);
                    for (int i = 0; i < HashSize; i++)
                        if (combined[SaltSize + i] != hash[i]) return false;
                    return true;
                }
            }
            catch { return false; }
        }

        // ── Anti-XSS helper ───────────────────────────────────────────────────
        public static string HtmlEncode(string input) =>
            System.Web.HttpUtility.HtmlEncode(input ?? "");

        // ── Request number generator ──────────────────────────────────────────
        public static string GenerateRequestNumber(string categoryCode)
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            string randPart = new Random().Next(1000, 9999).ToString();
            return $"{categoryCode.ToUpper()}-{datePart}-{randPart}";
        }

        // ── Basic email validation ────────────────────────────────────────────
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        }

        // ── Sanitize filename for upload ──────────────────────────────────────
        public static string SanitizeFileName(string filename)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                filename = filename.Replace(c, '_');
            return filename;
        }
    }
}

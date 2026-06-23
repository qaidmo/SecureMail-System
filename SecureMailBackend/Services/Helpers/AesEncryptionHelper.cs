using System.Security.Cryptography;
using System.Text;

namespace SecureMailBackend.Services.Helpers
{
    /// <summary>
    /// AES-256-CBC encryption helper for storing sensitive credentials at rest.
    /// IV is prepended to the ciphertext before Base64 encoding.
    /// </summary>
    public static class AesEncryptionHelper
    {
        /// <summary>
        /// Encrypts plaintext using AES-256-CBC with a random IV.
        /// Returns Base64-encoded string (IV + ciphertext).
        /// </summary>
        /// <param name="plainText">The text to encrypt.</param>
        /// <param name="base64Key">Base64-encoded 32-byte (256-bit) key.</param>
        public static string Encrypt(string plainText, string base64Key)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));

            byte[] key = Convert.FromBase64String(base64Key);
            if (key.Length != 32)
                throw new ArgumentException("AES key must be exactly 32 bytes (256 bits).", nameof(base64Key));

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV(); // Random IV per encryption

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to ciphertext for storage
            byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        /// <summary>
        /// Decrypts a Base64-encoded ciphertext (IV prepended) back to plaintext.
        /// </summary>
        /// <param name="cipherText">Base64-encoded string from Encrypt().</param>
        /// <param name="base64Key">Base64-encoded 32-byte (256-bit) key.</param>
        public static string Decrypt(string cipherText, string base64Key)
        {
            if (string.IsNullOrEmpty(cipherText))
                throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherText));

            byte[] key = Convert.FromBase64String(base64Key);
            if (key.Length != 32)
                throw new ArgumentException("AES key must be exactly 32 bytes (256 bits).", nameof(base64Key));

            byte[] fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV (first 16 bytes) and ciphertext (remainder)
            byte[] iv = new byte[16];
            byte[] cipher = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, 16);
            Buffer.BlockCopy(fullCipher, 16, cipher, 0, cipher.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Generates a new random 32-byte AES key and returns it as a Base64 string.
        /// Useful for initial setup / key rotation.
        /// </summary>
        public static string GenerateKey()
        {
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return Convert.ToBase64String(key);
        }
    }
}

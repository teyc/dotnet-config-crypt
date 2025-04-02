

// CryptoService.cs
using System.Security.Cryptography;
using System.Text;

namespace ConfigCrypt
{
    public class CryptoService
    {
        private readonly byte[] _key;
        
        // AES-GCM constants
        private const int NonceSize = 12; // 96 bits
        private const int TagSize = 16;   // 128 bits
        
        public CryptoService(string base64Key)
        {
            _key = Convert.FromBase64String(base64Key);
            
            // Ensure key is 256 bits (32 bytes)
            if (_key.Length != 32)
            {
                throw new ArgumentException("AES key must be 256 bits (32 bytes)");
            }
        }

        public string Encrypt(string plainText)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes;
            
            // Generate a new random nonce for each encryption
            byte[] nonce = new byte[NonceSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }
            
            // Use AesGcm for authenticated encryption
            using (var aesGcm = new AesGcm(_key))
            {
                byte[] cipherText = new byte[plainBytes.Length];
                byte[] tag = new byte[TagSize];
                
                aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);
                
                // Combine nonce + tag + ciphertext for storage
                cipherBytes = new byte[NonceSize + TagSize + cipherText.Length];
                Buffer.BlockCopy(nonce, 0, cipherBytes, 0, NonceSize);
                Buffer.BlockCopy(tag, 0, cipherBytes, NonceSize, TagSize);
                Buffer.BlockCopy(cipherText, 0, cipherBytes, NonceSize + TagSize, cipherText.Length);
            }
            
            // Format for storage in JSON: @config-crypt(base64data)
            return "@config-crypt(" + Convert.ToBase64String(cipherBytes) + ")";
        }

        public string Decrypt(string encryptedText)
        {
            // Remove the prefix and suffix
            if (!encryptedText.StartsWith("@config-crypt(") || !encryptedText.EndsWith(")"))
            {
                throw new ArgumentException("Invalid encrypted format");
            }
            
            string base64Content = encryptedText.Substring(13, encryptedText.Length - 14);
            byte[] cipherBytes = Convert.FromBase64String(base64Content);
            
            if (cipherBytes.Length < NonceSize + TagSize)
            {
                throw new ArgumentException("Encrypted data is too short");
            }
            
            // Extract nonce, tag, and ciphertext
            byte[] nonce = new byte[NonceSize];
            byte[] tag = new byte[TagSize];
            byte[] cipherText = new byte[cipherBytes.Length - NonceSize - TagSize];
            
            Buffer.BlockCopy(cipherBytes, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(cipherBytes, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(cipherBytes, NonceSize + TagSize, cipherText, 0, cipherText.Length);
            
            // Decrypt the data
            byte[] plainBytes = new byte[cipherText.Length];
            using (var aesGcm = new AesGcm(_key))
            {
                aesGcm.Decrypt(nonce, cipherText, tag, plainBytes);
            }
            
            return Encoding.UTF8.GetString(plainBytes);
        }

        public bool IsEncrypted(string value)
        {
            return value != null && value.StartsWith("@config-crypt(") && value.EndsWith(")");
        }
    }
}

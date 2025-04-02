// Tests/CryptoServiceTests.cs
namespace ConfigCrypt.UnitTests
{
    public class CryptoServiceTests
    {
        private readonly CryptoService _cryptoService;

        public CryptoServiceTests()
        {
            // Generate a test key
            byte[] keyBytes = new byte[32]; // 256 bits
            new Random(42).NextBytes(keyBytes); // Deterministic for tests
            string testKey = Convert.ToBase64String(keyBytes);
            
            _cryptoService = new CryptoService(testKey);
        }

        [Fact]
        public void EncryptDecrypt_SimpleString_ReturnsOriginal()
        {
            // Arrange
            string original = "test-password-123";
            
            // Act
            string encrypted = _cryptoService.Encrypt(original);
            string decrypted = _cryptoService.Decrypt(encrypted);
            
            // Assert
            Assert.NotEqual(original, encrypted);
            Assert.StartsWith("@config-crypt(", encrypted);
            Assert.EndsWith(")", encrypted);
            Assert.Equal(original, decrypted);
        }

        [Fact]
        public void IsEncrypted_WithEncryptedString_ReturnsTrue()
        {
            // Arrange
            string encrypted = "@config-crypt(base64data)";
            
            // Act
            bool result = _cryptoService.IsEncrypted(encrypted);
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsEncrypted_WithPlainString_ReturnsFalse()
        {
            // Arrange
            string plainText = "normal-text";
            
            // Act
            bool result = _cryptoService.IsEncrypted(plainText);
            
            // Assert
            Assert.False(result);
        }
    }
}

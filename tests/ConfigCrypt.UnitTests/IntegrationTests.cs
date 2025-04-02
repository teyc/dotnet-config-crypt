// Tests/IntegrationTests.cs
using ConfigCrypt;
using ConfigCrypt.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ConfigCrypt.Tests
{
    public class IntegrationTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _testInputFile;
        private readonly string _testOutputFile;
        private readonly ConfigCryptConfig _config;
        private readonly CryptoService _cryptoService;
        private readonly JsonProcessor _jsonProcessor;

        public IntegrationTests()
        {
            // Setup test directory and files
            _testDirectory = Path.Combine(Path.GetTempPath(), $"config-crypt-tests-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            _testInputFile = Path.Combine(_testDirectory, "test-input.json");
            _testOutputFile = Path.Combine(_testDirectory, "test-output.json");

            // Generate a test key
            byte[] keyBytes = new byte[32]; // 256 bits
            new Random(42).NextBytes(keyBytes); // Deterministic for tests
            string testKey = Convert.ToBase64String(keyBytes);

            _config = new ConfigCryptConfig
            {
                AesKey = testKey,
                Secrets = new List<string> { "key", "password", "secret" },
                Exclude = new List<string> { "$.publicData" }
            };

            _cryptoService = new CryptoService(testKey);
            _jsonProcessor = new JsonProcessor(_cryptoService, _config);
        }

        public void Dispose()
        {
            // Clean up test files
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void ComplexNestedJson_FullEncryptDecryptCycle_RestoresOriginal()
        {
            // Arrange
            string complexJson = @"{
                ""application"": {
                    ""name"": ""TestApp"",
                    ""environment"": ""development"",
                    ""security"": {
                        ""apiKey"": ""api-key-12345"",
                        ""tokenSecret"": ""secret-token-abcde"",
                        ""publicKey"": ""public-key-xyz""
                    }
                },
                ""database"": {
                    ""connectionString"": ""Server=myserver;Database=mydb;User Id=admin;Password=db-password-123"",
                    ""readOnlyConnection"": ""Server=readonly;Database=mydb;User Id=reader;Password=reader-pwd""
                },
                ""services"": [
                    {
                        ""name"": ""Service1"",
                        ""endpoint"": ""https://service1.example.com"",
                        ""credentials"": {
                            ""username"": ""service1-user"",
                            ""password"": ""service1-password""
                        }
                    },
                    {
                        ""name"": ""Service2"",
                        ""endpoint"": ""https://service2.example.com"",
                        ""apiKeys"": [""key1"", ""key2"", ""key3""]
                    }
                ],
                ""publicData"": {
                    ""apiKey"": ""this-should-not-be-encrypted"",
                    ""secretKey"": ""this-also-should-not-be-encrypted""
                }
            }";

            File.WriteAllText(_testInputFile, complexJson);

            // Act - Encrypt
            string encryptedJson = _jsonProcessor.ProcessForEncryption(complexJson);
            File.WriteAllText(_testOutputFile, encryptedJson);

            // Decrypt
            string decryptedJson = _jsonProcessor.ProcessForDecryption(encryptedJson);

            // Assert
            var originalObj = JObject.Parse(complexJson);
            var decryptedObj = JObject.Parse(decryptedJson);

            // Check encrypted values were actually encrypted
            var encryptedObj = JObject.Parse(encryptedJson);
            Assert.True(_cryptoService.IsEncrypted(encryptedObj["application"]["security"]["apiKey"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(encryptedObj["application"]["security"]["tokenSecret"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(encryptedObj["database"]["connectionString"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(encryptedObj["services"][0]["credentials"]["password"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(encryptedObj["services"][1]["apiKeys"][0].ToString()));

            // Check excluded paths weren't encrypted
            Assert.False(_cryptoService.IsEncrypted(encryptedObj["publicData"]["apiKey"].ToString()));
            Assert.False(_cryptoService.IsEncrypted(encryptedObj["publicData"]["secretKey"].ToString()));

            // Check decryption restored original values
            Assert.Equal(originalObj["application"]["security"]["apiKey"].ToString(),
                        decryptedObj["application"]["security"]["apiKey"].ToString());
            Assert.Equal(originalObj["database"]["connectionString"].ToString(),
                        decryptedObj["database"]["connectionString"].ToString());
            Assert.Equal(originalObj["services"][0]["credentials"]["password"].ToString(),
                        decryptedObj["services"][0]["credentials"]["password"].ToString());
            Assert.Equal(originalObj["services"][1]["apiKeys"][0].ToString(),
                        decryptedObj["services"][1]["apiKeys"][0].ToString());
        }

        [Fact]
        public void ProcessFile_WithValuesContainingSecretWords_EncryptsThoseValues()
        {
            // Arrange
            string json = @"{
                ""server"": ""production"",
                ""url"": ""https://example.com?password=abc123"",
                ""normal"": ""regular value"",
                ""config"": {
                    ""setting"": ""This contains a secret inside the text""
                }
            }";

            File.WriteAllText(_testInputFile, json);

            // Act
            string encrypted = _jsonProcessor.ProcessForEncryption(json);
            var encryptedObj = JObject.Parse(encrypted);

            // Assert
            Assert.False(_cryptoService.IsEncrypted(encryptedObj["server"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(encryptedObj["url"].ToString())); // Contains "password="
            Assert.False(_cryptoService.IsEncrypted(encryptedObj["normal"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(encryptedObj["config"]["setting"].ToString())); // Contains "secret"
        }
    }
}

using ConfigCrypt;
using ConfigCrypt.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Xunit;

namespace ConfigCrypt.UnitTests
{
    public class JsonProcessorTests
    {
        private readonly CryptoService _cryptoService;
        private readonly JsonProcessor _jsonProcessor;

        public JsonProcessorTests()
        {
            // Generate a test key
            byte[] keyBytes = new byte[32]; // 256 bits
            new Random(42).NextBytes(keyBytes); // Deterministic for tests
            string testKey = Convert.ToBase64String(keyBytes);
            
            _cryptoService = new CryptoService(testKey);
            
            var config = new ConfigCryptConfig
            {
                AesKey = testKey,
                Secrets = new List<string> { "key", "password", "secret" },
                Exclude = new List<string> { "$.publicData" }
            };
            
            _jsonProcessor = new JsonProcessor(_cryptoService, config);
        }

        [Fact]
        public void ProcessForEncryption_SimpleSecrets_EncryptsValues()
        {
            // Arrange
            string json = @"{
                ""apiKey"": ""my-api-key"",
                ""username"": ""john"",
                ""password"": ""secret123""
            }";
            
            // Act
            string encrypted = _jsonProcessor.ProcessForEncryption(json);
            var result = JObject.Parse(encrypted);
            
            // Assert
            Assert.True(_cryptoService.IsEncrypted(result["apiKey"].ToString()));
            Assert.False(_cryptoService.IsEncrypted(result["username"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["password"].ToString()));
        }

        [Fact]
        public void ProcessForEncryption_NestedJson_EncryptsNestedValues()
        {
            // Arrange
            string json = @"{
                ""app"": {
                    ""name"": ""MyApp"",
                    ""credentials"": {
                        ""apiKey"": ""my-api-key"",
                        ""secret"": ""top-secret""
                    }
                },
                ""database"": {
                    ""connectionString"": ""Server=localhost;Password=dbpass""
                }
            }";
            
            // Act
            string encrypted = _jsonProcessor.ProcessForEncryption(json);
            var result = JObject.Parse(encrypted);
            
            // Assert
            Assert.False(_cryptoService.IsEncrypted(result["app"]["name"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["app"]["credentials"]["apiKey"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["app"]["credentials"]["secret"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["database"]["connectionString"].ToString()));
        }

        [Fact]
        public void ProcessForEncryption_WithExcludedPath_DoesNotEncrypt()
        {
            // Arrange
            string json = @"{
                ""publicData"": {
                    ""apiKey"": ""public-api-key"",
                    ""secret"": ""not-really-secret""
                },
                ""privateData"": {
                    ""apiKey"": ""private-api-key""
                }
            }";
            
            // Act
            string encrypted = _jsonProcessor.ProcessForEncryption(json);
            var result = JObject.Parse(encrypted);
            
            // Assert
            Assert.False(_cryptoService.IsEncrypted(result["publicData"]["apiKey"].ToString()));
            Assert.False(_cryptoService.IsEncrypted(result["publicData"]["secret"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["privateData"]["apiKey"].ToString()));
        }

        [Fact]
        public void ProcessForEncryption_WithArrays_EncryptsArrayValues()
        {
            // Arrange
            string json = @"{
                ""users"": [
                    {
                        ""name"": ""John"",
                        ""password"": ""john123""
                    },
                    {
                        ""name"": ""Jane"",
                        ""password"": ""jane456""
                    }
                ],
                ""apiKeys"": [""key1"", ""key2"", ""normal""]
            }";
            
            // Act
            string encrypted = _jsonProcessor.ProcessForEncryption(json);
            var result = JObject.Parse(encrypted);
            
            // Assert
            Assert.False(_cryptoService.IsEncrypted(result["users"][0]["name"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["users"][0]["password"].ToString()));
            Assert.False(_cryptoService.IsEncrypted(result["users"][1]["name"].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["users"][1]["password"].ToString()));
            
            Assert.True(_cryptoService.IsEncrypted(result["apiKeys"][0].ToString()));
            Assert.True(_cryptoService.IsEncrypted(result["apiKeys"][1].ToString()));
            Assert.False(_cryptoService.IsEncrypted(result["apiKeys"][2].ToString()));
        }

        [Fact]
        public void ProcessForDecryption_RestoresOriginalValues()
        {
            // Arrange
            string original = @"{
                ""apiKey"": ""my-api-key"",
                ""username"": ""john"",
                ""password"": ""secret123"",
                ""nested"": {
                    ""secretKey"": ""nested-secret""
                }
            }";
            
            // First encrypt it
            string encrypted = _jsonProcessor.ProcessForEncryption(original);
            
            // Act
            string decrypted = _jsonProcessor.ProcessForDecryption(encrypted);
            
            // Assert
            // Parse both to normalize formatting
            var originalObj = JObject.Parse(original);
            var decryptedObj = JObject.Parse(decrypted);
            
            Assert.Equal(originalObj["apiKey"].ToString(), decryptedObj["apiKey"].ToString());
            Assert.Equal(originalObj["password"].ToString(), decryptedObj["password"].ToString());
            Assert.Equal(originalObj["nested"]["secretKey"].ToString(), decryptedObj["nested"]["secretKey"].ToString());
        }
    }
}


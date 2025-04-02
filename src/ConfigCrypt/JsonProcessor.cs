// JsonProcessor.cs
using ConfigCrypt.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigCrypt
{
    public class JsonProcessor
    {
        private readonly CryptoService _cryptoService;
        private readonly ConfigCryptConfig _config;
        private readonly HashSet<string> _excludePaths;

        public JsonProcessor(CryptoService cryptoService, ConfigCryptConfig config)
        {
            _cryptoService = cryptoService;
            _config = config;
            _excludePaths = new HashSet<string>(config.Exclude);
        }

        public string ProcessForEncryption(string jsonContent)
        {
            var jsonObject = JToken.Parse(jsonContent);
            ProcessTokenForEncryption(jsonObject, "$");
            return jsonObject.ToString(Formatting.Indented);
        }

        public string ProcessForDecryption(string jsonContent)
        {
            var jsonObject = JToken.Parse(jsonContent);
            ProcessTokenForDecryption(jsonObject, "$");
            return jsonObject.ToString(Formatting.Indented);
        }

        private void ProcessTokenForEncryption(JToken token, string path)
        {
            if (IsPathExcluded(path))
            {
                return;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var property in token.Children<JProperty>().ToList())
                    {
                        string propertyPath = $"{path}.{property.Name}";
                        
                        // If the property name matches any secret pattern
                        bool shouldEncryptValue = _config.Secrets.Any(secret => 
                            property.Name.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0);
                        
                        // Process the property value
                        if (property.Value.Type == JTokenType.String)
                        {
                            string stringValue = property.Value.Value<string>();
                            
                            // Also check if the string value itself contains any secret patterns
                            if (!shouldEncryptValue && stringValue != null)
                            {
                                shouldEncryptValue = _config.Secrets.Any(secret => 
                                    stringValue.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0);
                            }
                            
                            // Skip already encrypted values
                            if (shouldEncryptValue && !_cryptoService.IsEncrypted(stringValue))
                            {
                                property.Value = _cryptoService.Encrypt(stringValue);
                            }
                        }
                        else
                        {
                            // Recursively process nested objects/arrays
                            ProcessTokenForEncryption(property.Value, propertyPath);
                        }
                    }
                    break;
                
                case JTokenType.Array:
                    int index = 0;
                    foreach (var item in token.Children().ToArray())
                    {
                        ProcessTokenForEncryption(item, $"{path}[{index}]");
                        index++;
                    }
                    break;
                
                case JTokenType.String:
                    // Handle direct string values in arrays
                    var parentArray = token.Parent as JArray;
                    if (parentArray != null)
                    {
                        string stringValue = token.Value<string>();
                        
                        // Check if the string value contains any secret patterns
                        bool shouldEncrypt = _config.Secrets.Any(secret => 
                            stringValue != null && stringValue.IndexOf(secret, StringComparison.OrdinalIgnoreCase) >= 0);
                        
                        if (shouldEncrypt && !_cryptoService.IsEncrypted(stringValue))
                        {
                            int tokenIndex = parentArray.IndexOf(token);
                            parentArray[tokenIndex] = _cryptoService.Encrypt(stringValue);
                        }
                    }
                    break;
            }
        }

        private void ProcessTokenForDecryption(JToken token, string path)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var property in token.Children<JProperty>().ToList())
                    {
                        string propertyPath = $"{path}.{property.Name}";
                        
                        if (property.Value.Type == JTokenType.String)
                        {
                            string stringValue = property.Value.Value<string>();
                            if (_cryptoService.IsEncrypted(stringValue))
                            {
                                if (!IsPathExcluded(propertyPath))
                                {
                                    property.Value = _cryptoService.Decrypt(stringValue);
                                }
                            }
                        }
                        else
                        {
                            ProcessTokenForDecryption(property.Value, propertyPath);
                        }
                    }
                    break;
                
                case JTokenType.Array:
                    int index = 0;
                    foreach (var item in token.Children().ToArray())
                    {
                        ProcessTokenForDecryption(item, $"{path}[{index}]");
                        index++;
                    }
                    break;
                
                case JTokenType.String:
                    var parentArray = token.Parent as JArray;
                    if (parentArray != null)
                    {
                        string stringValue = token.Value<string>();
                        if (_cryptoService.IsEncrypted(stringValue) && !IsPathExcluded(path))
                        {
                            int tokenIndex = parentArray.IndexOf(token);
                            parentArray[tokenIndex] = _cryptoService.Decrypt(stringValue);
                        }
                    }
                    break;
            }
        }

        private bool IsPathExcluded(string path)
        {
            // Check if the current path is in the exclude list
            // For simplicity, we're doing a direct match here
            // A more sophisticated implementation would use proper JSONPath evaluation
            return _excludePaths.Contains(path);
        }
    }
}

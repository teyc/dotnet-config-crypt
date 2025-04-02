
// ConfigManager.cs
using System.Buffers.Text;
using System.Text;
using ConfigCrypt.Models;
using Newtonsoft.Json;
using PanoramicData.ConsoleExtensions;

namespace ConfigCrypt
{
    public class ConfigManager
    {
        private readonly string _configDirectoryPath;
        private readonly string _configFilePath;

        public ConfigManager()
        {
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _configDirectoryPath = Path.Combine(homeDirectory, ".config-crypt");
            _configFilePath = Path.Combine(_configDirectoryPath, "config.json");
        }

        public ConfigCryptConfig LoadConfig(bool interactive)
        {
            if (!Directory.Exists(_configDirectoryPath))
            {
                Directory.CreateDirectory(_configDirectoryPath);
            }

            if (File.Exists(_configFilePath))
            {
                if (interactive)
                {
                    Console.Error.WriteLine($"Encryption key is already set in the configuration file. Please delete {_configFilePath} to reset.");
                }
            }
            else
            {
                string aesKey;

                if (interactive)
                {
                    Console.WriteLine("No configuration file found. Please enter a encryption/decryption key:");
                    aesKey = ConsolePlus.ReadPassword();
                    Console.WriteLine();
                    try
                    {
                        Convert.FromBase64String(aesKey);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("Invalid key format. Please enter a valid Base64 encoded key.");
                        throw;
                    }
                }
                else
                {
                    aesKey = GenerateRandomKey();
                }

                // Create default config
                var defaultConfig = new ConfigCryptConfig
                {
                    AesKey = aesKey,
                    Secrets = new List<string> { "key", "password", "secret" },
                    Exclude = new List<string>()
                };

                SaveConfig(defaultConfig);
                Console.WriteLine($"Created default configuration at {_configFilePath}");
                return defaultConfig;
            }

            try
            {
                string jsonConfig = File.ReadAllText(_configFilePath);
                var config = JsonConvert.DeserializeObject<ConfigCryptConfig>(jsonConfig);

                if (config == null)
                {
                    throw new InvalidOperationException("Failed to deserialize configuration.");
                }

                // Ensure default values if null
                config.Secrets ??= new List<string> { "key", "password", "secret" };
                config.Exclude ??= new List<string>();

                // Generate key if missing
                if (string.IsNullOrEmpty(config.AesKey))
                {
                    config.AesKey = GenerateRandomKey();
                    SaveConfig(config);
                    Console.WriteLine("Generated a new AES key in configuration.");
                }

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                Console.WriteLine("Creating a new default configuration.");

                var defaultConfig = new ConfigCryptConfig
                {
                    AesKey = GenerateRandomKey(),
                    Secrets = new List<string> { "key", "password", "secret" },
                    Exclude = new List<string>()
                };

                SaveConfig(defaultConfig);
                return defaultConfig;
            }
        }

        public void SaveConfig(ConfigCryptConfig config)
        {
            try
            {
                string jsonConfig = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(_configFilePath, jsonConfig);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                throw;
            }
        }

        private string GenerateRandomKey()
        {
            // Generate a 256-bit (32 byte) random key
            byte[] keyBytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            return Convert.ToBase64String(keyBytes);
        }
    }
}

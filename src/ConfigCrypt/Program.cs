
// Program.cs
using CommandLineParser;
using CommandLineParser.Arguments;
using ConfigCrypt;
using ConfigCrypt.Models;
using System;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        try
        {
            var parser = new CommandLineParser.CommandLineParser();
            
            var cryptCommand = new ValueArgument<string>('c', "crypt", "Encrypt JSON file");
            var decryptCommand = new ValueArgument<string>('d', "decrypt", "Decrypt JSON file");
            var outputFile = new ValueArgument<string>('o', "output", "Output file path");
            
            parser.Arguments.Add(cryptCommand);
            parser.Arguments.Add(decryptCommand);
            parser.Arguments.Add(outputFile);
            
            // Parse the arguments
            parser.ParseCommandLine(args);

            if (args.Length == 0 || (args.Length > 0 && (args[0] == "--help" || args[0] == "-h")))
            {
                ShowHelp();
                return 0;
            }

            var configManager = new ConfigManager();
            var config = configManager.LoadConfig();
            
            if (string.IsNullOrEmpty(config.AesKey))
            {
                Console.WriteLine("Error: No AES key found in configuration.");
                return 1;
            }
            
            var cryptoService = new CryptoService(config.AesKey);
            var jsonProcessor = new JsonProcessor(cryptoService, config);
            
            // Handle command: crypt
            if (args.Length >= 2 && args[0] == "crypt")
            {
                string inputFile = args[1];
                string outputFilePath = args.Length >= 3 ? args[2] : Path.ChangeExtension(inputFile, ".encrypted.json");
                
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"Error: Input file '{inputFile}' does not exist.");
                    return 1;
                }
                
                string jsonContent = File.ReadAllText(inputFile);
                
                try
                {
                    string processedJson = jsonProcessor.ProcessForEncryption(jsonContent);
                    File.WriteAllText(outputFilePath, processedJson);
                    Console.WriteLine($"Successfully encrypted JSON to {outputFilePath}");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error encrypting JSON: {ex.Message}");
                    return 1;
                }
            }
            
            // Handle command: decrypt
            else if (args.Length >= 2 && args[0] == "decrypt")
            {
                string inputFile = args[1];
                string outputFilePath = args.Length >= 3 ? args[2] : 
                    inputFile.EndsWith(".encrypted.json") ? 
                        inputFile.Replace(".encrypted.json", ".json") : 
                        Path.ChangeExtension(inputFile, ".decrypted.json");
                
                if (!File.Exists(inputFile))
                {
                    Console.WriteLine($"Error: Input file '{inputFile}' does not exist.");
                    return 1;
                }
                
                string jsonContent = File.ReadAllText(inputFile);
                
                try
                {
                    string processedJson = jsonProcessor.ProcessForDecryption(jsonContent);
                    File.WriteAllText(outputFilePath, processedJson);
                    Console.WriteLine($"Successfully decrypted JSON to {outputFilePath}");
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decrypting JSON: {ex.Message}");
                    return 1;
                }
            }
            else
            {
                ShowHelp();
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
    
    static void ShowHelp()
    {
        Console.WriteLine("config-crypt - JSON file encryption and decryption tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  config-crypt crypt <inputFile.json> [outputFile.encrypted.json]");
        Console.WriteLine("  config-crypt decrypt <inputFile.encrypted.json> [outputFile.json]");
        Console.WriteLine();
        Console.WriteLine("Configuration is stored in ~/.config-crypt/config.json");
    }
}

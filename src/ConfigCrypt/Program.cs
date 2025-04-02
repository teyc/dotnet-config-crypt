// Program.cs
using CommandLine;
using ConfigCrypt;

class Program
{
    // Define the options for the commands
    [Verb("crypt", HelpText = "Encrypt a JSON file")]
    class CryptOptions
    {
        [Value(0, MetaName = "input", Required = true, HelpText = "Input JSON file path")]
        public string InputFile { get; set; }

        [Value(1, MetaName = "output", Required = false, HelpText = "Output encrypted JSON file path")]
        public string OutputFile { get; set; }

        [Option(HelpText = "Run in interactive mode to prompt for password", Default = false)]
        public bool Interactive { get; set; } = false;
    }

    [Verb("decrypt", HelpText = "Decrypt a JSON file")]
    class DecryptOptions
    {
        [Value(0, MetaName = "input", Required = true, HelpText = "Input encrypted JSON file path")]
        public string InputFile { get; set; }

        [Value(1, MetaName = "output", Required = false, HelpText = "Output decrypted JSON file path")]
        public string OutputFile { get; set; }

        [Option(HelpText = "Run in interactive mode to prompt for password", Default = false)]
        public bool Interactive { get; set; } = false;
    }

    static int Main(string[] args)
    {
        return Parser.Default.ParseArguments<CryptOptions, DecryptOptions>(args)
            .MapResult(
                (CryptOptions opts) => RunCryptCommand(opts),
                (DecryptOptions opts) => RunDecryptCommand(opts),
                errs => 1);
    }

    static int RunCryptCommand(CryptOptions opts)
    {
        try
        {
            string inputFile = opts.InputFile;
            string outputFilePath = opts.OutputFile ?? Path.ChangeExtension(inputFile, ".encrypted.json");

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Error: Input file '{inputFile}' does not exist.");
                return 1;
            }

            var configManager = new ConfigManager();
            var config = configManager.LoadConfig(opts.Interactive);

            if (string.IsNullOrEmpty(config.AesKey))
            {
                Console.WriteLine("Error: No AES key found in configuration.");
                return 1;
            }

            var cryptoService = new CryptoService(config.AesKey);
            var jsonProcessor = new JsonProcessor(cryptoService, config);

            string jsonContent = File.ReadAllText(inputFile);
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

    static int RunDecryptCommand(DecryptOptions opts)
    {
        try
        {
            string inputFile = opts.InputFile;
            string outputFilePath = opts.OutputFile;

            if (outputFilePath == null)
            {
                outputFilePath = inputFile.EndsWith(".encrypted.json") ?
                    inputFile.Replace(".encrypted.json", ".json") :
                    Path.ChangeExtension(inputFile, ".decrypted.json");
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Error: Input file '{inputFile}' does not exist.");
                return 1;
            }

            var configManager = new ConfigManager();
            var config = configManager.LoadConfig(opts.Interactive);

            if (string.IsNullOrEmpty(config.AesKey))
            {
                Console.WriteLine("Error: No AES key found in configuration.");
                return 1;
            }

            var cryptoService = new CryptoService(config.AesKey);
            var jsonProcessor = new JsonProcessor(cryptoService, config);

            string jsonContent = File.ReadAllText(inputFile);
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
}
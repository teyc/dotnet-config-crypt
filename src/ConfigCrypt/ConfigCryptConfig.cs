namespace ConfigCrypt.Models
{
    public class ConfigCryptConfig
    {
        public string? AesKey { get; set; }
        public List<string> Secrets { get; set; } = new List<string> { "key", "password", "secret" };
        public List<string> Exclude { get; set; } = new List<string>();
    }
}
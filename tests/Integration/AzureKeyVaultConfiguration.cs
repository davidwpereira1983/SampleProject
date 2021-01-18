namespace Company.TestProject.Tests.Integration
{
    public class AzureKeyVaultConfiguration
    {
        public bool Enable { get; set; }
        public string Url { get; set; }
        public string Prefix { get; set; }
        public int ReloadIntervalInMinutes { get; set; }
    }
}

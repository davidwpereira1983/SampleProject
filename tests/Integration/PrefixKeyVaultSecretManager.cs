using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Company.TestProject.Tests.Integration
{
    public class PrefixKeyVaultSecretManager : KeyVaultSecretManager
    {
        private readonly string prefix;

        public PrefixKeyVaultSecretManager(string prefix)
        {
            this.prefix = $"{prefix}";
        }

        public override bool Load(SecretProperties secret)
        {
            return secret.Name.StartsWith(this.prefix);
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            return secret.Name
                .Substring(this.prefix.Length)
                .Replace("--", ConfigurationPath.KeyDelimiter);
        }
    }
}

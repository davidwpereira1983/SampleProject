using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Company.TestProject.WebApi
{
    public class AzureKeyVaultConfiguration
    {
        public bool Enable { get; set; }
        public string Url { get; set; }
        public string Prefix { get; set; }
        public int ReloadIntervalInMinutes { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Company.TestProject.WebApi.HealthCheck
{
    public class MemoryCheckOptions
    {
        // Failure threshold (in bytes)
        public long Threshold { get; set; } = 1024L * 1024L * 1024L;
    }
}

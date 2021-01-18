using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.TestProject.Shared.BrokenRules
{
    public class BrokenRuleException : ApplicationException
    {
        public BrokenRuleException(IEnumerable<BrokenRule> brokenRules)
        {
            this.BrokenRules = brokenRules ?? throw new ArgumentNullException(nameof(brokenRules));
        }

        public IEnumerable<BrokenRule> BrokenRules { get; }
    }
}

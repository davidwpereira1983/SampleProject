using System.Collections.Generic;

namespace Company.TestProject.Shared.BrokenRules
{
    public static class BrokenRuleExtensions
    {
        public static BrokenRuleException ToException(this BrokenRule brokenRule)
        {
            var brokenRules = new[] { brokenRule };
            return brokenRules.ToException();
        }

        public static BrokenRuleException ToException(this IEnumerable<BrokenRule> brokenRules)
        {
            return new BrokenRuleException(brokenRules);
        }
    }
}

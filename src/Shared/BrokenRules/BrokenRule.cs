using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Company.TestProject.Shared.BrokenRules
{
    public abstract class BrokenRule
    {
        protected BrokenRule(string errorCode, BrokenRuleSeverity severity, params object[] prms)
        {
            this.Severity = severity;
            this.ErrorCode = errorCode;
            this.Prms = prms;
        }

        public BrokenRuleSeverity Severity { get; }
        public string ErrorCode { get; }
        public object[] Prms { get; }
    }
}

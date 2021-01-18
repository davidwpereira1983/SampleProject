using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace Company.TestProject.WebApi
{
    public class TraceIdentifierEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var currentActivity = Activity.Current;

            var traceIdProperty = propertyFactory.CreateProperty("TraceId", currentActivity?.TraceId);
            var spanIdProperty = propertyFactory.CreateProperty("SpanId", currentActivity?.SpanId);

            logEvent.AddOrUpdateProperty(traceIdProperty);
            logEvent.AddOrUpdateProperty(spanIdProperty);
        }
    }
}

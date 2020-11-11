using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Events;

namespace Serilog.Sinks.Fluentd
{
    internal interface IBatchedLogEventSink : IDisposable
    {
        Task PushEventsAsync(IEnumerable<LogEvent> events, CancellationToken cancellationToken);
    }
}
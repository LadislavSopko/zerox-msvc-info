using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Msvc.Info.Core.Logging
{
    
    public class TraceSourceLogger : ILogger
    {
        private readonly string _name;
        private readonly TraceSource _traceSource;

        public TraceSourceLogger(string name, TraceSource traceSource)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _traceSource = traceSource ?? throw new ArgumentNullException(nameof(traceSource));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel switch
            {
                LogLevel.Trace => _traceSource.Switch.ShouldTrace(TraceEventType.Verbose),
                LogLevel.Debug => _traceSource.Switch.ShouldTrace(TraceEventType.Verbose),
                LogLevel.Information => _traceSource.Switch.ShouldTrace(TraceEventType.Information),
                LogLevel.Warning => _traceSource.Switch.ShouldTrace(TraceEventType.Warning),
                LogLevel.Error => _traceSource.Switch.ShouldTrace(TraceEventType.Error),
                LogLevel.Critical => _traceSource.Switch.ShouldTrace(TraceEventType.Critical),
                _ => false,
            };

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    _traceSource.TraceEvent(TraceEventType.Verbose, eventId.Id, message);
                    break;
                case LogLevel.Information:
                    _traceSource.TraceEvent(TraceEventType.Information, eventId.Id, message);
                    break;
                case LogLevel.Warning:
                    _traceSource.TraceEvent(TraceEventType.Warning, eventId.Id, message);
                    break;
                case LogLevel.Error:
                    _traceSource.TraceEvent(TraceEventType.Error, eventId.Id, message);
                    break;
                case LogLevel.Critical:
                    _traceSource.TraceEvent(TraceEventType.Critical, eventId.Id, message);
                    break;
                default:
                    _traceSource.TraceEvent(TraceEventType.Verbose, eventId.Id, message);
                    break;
            }

            if (exception != null)
                _traceSource.TraceEvent(TraceEventType.Error, eventId.Id, $"Exception: {exception}");
        }
    }
}

using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Msvc.Info.Core.Logging
{

    public class TraceSourceLoggerProvider : ILoggerProvider
    {
        private readonly TraceSource _traceSource;

        public TraceSourceLoggerProvider(TraceSource traceSource)
        {
            _traceSource = traceSource;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TraceSourceLogger(categoryName, _traceSource);
        }

        public void Dispose()
        {
            _traceSource.Flush();
            _traceSource.Close();
        }
    }
}

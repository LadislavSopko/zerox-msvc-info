using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Msvc.Info.Logging
{
    [VisualStudioContribution]
    public class ExtensionLogger : IExtensionLogger, IDisposable
    {
        private readonly ILogger<ExtensionLogger> _logger;
        private readonly IServiceProvider _sp;

        private IVsOutputWindow? _outputWindow;
        private IVsOutputWindowPane? _pane;
        private  Guid _paneGuid = new Guid("12345678-1234-1234-1234-123456789012");

        public ExtensionLogger(ILogger<ExtensionLogger> logger, IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;
            InitializePane();
        }

        private void InitializePane()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _outputWindow = _sp.GetService<IVsOutputWindow>();

                if (_outputWindow != null)
                {
                    // Try to get existing pane
                    _outputWindow.GetPane(ref _paneGuid, out _pane);

                    if (_pane == null)
                    {
                        // Create new pane
                        _outputWindow.CreatePane(ref _paneGuid, "MCP Extension", 1, 1);
                        _outputWindow.GetPane(ref _paneGuid, out _pane);
                    }
                }
            });
        }

        public void LogInfo(string message)
        {
            _logger.LogInformation(message);
            WriteToOutputPane("INFO", message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                _logger.LogError(ex, message);
            else
                _logger.LogError(message);

            var errorMsg = ex != null ? $"{message}: {ex.Message}" : message;
            WriteToOutputPane("ERROR", errorMsg);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
            WriteToOutputPane("WARN", message);
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
            WriteToOutputPane("DEBUG", message);
        }

        private void WriteToOutputPane(string level, string message)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _pane?.OutputStringThreadSafe($"[{level}] {DateTime.Now:HH:mm:ss} - {message}\n");
                _pane?.Activate();
            });
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}

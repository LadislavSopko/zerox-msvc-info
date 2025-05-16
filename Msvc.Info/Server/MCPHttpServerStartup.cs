using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info.Server
{
    /// <summary>
    /// Extension part that manages the HTTP server lifecycle
    /// </summary>
    [VisualStudioContribution]
    internal class MCPHttpServerStartup : ExtensionPart, IDisposable
    {
        private readonly MCPHttpServer _httpServer;
        private readonly MCPHttpServerConfiguration _configuration;
        private readonly ILogger<MCPHttpServerStartup> _logger;

        public MCPHttpServerStartup(
            IMCPService mcpService,
            ILogger<MCPHttpServerStartup> logger)
        {
            _logger = logger;
            _configuration = new MCPHttpServerConfiguration();
            _httpServer = new MCPHttpServer(mcpService, logger, _configuration);
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await base.InitializeAsync(cancellationToken);

            try
            {
                // Start the HTTP server if enabled
                if (_configuration.Enabled)
                {
                    _httpServer.Start(cancellationToken);
                    _logger.LogInformation("MCP HTTP server initialized successfully at {BaseUrl}", _configuration.BaseUrl);
                }
                else
                {
                    _logger.LogInformation("MCP HTTP server is disabled in configuration");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP HTTP server");
                // Don't throw - allow extension to continue without HTTP
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            // Dispose of the HTTP server if it was started
            if (_httpServer != null)
            {
                _httpServer?.Dispose();
                _logger.LogInformation("MCP HTTP server stopped successfully");
            }
            // Call base Dispose method
            // to ensure proper cleanup of the extension part
            // and any other resources it may hold
            // (if applicable in your context)
            // Note: The base class may not have a Dispose method, but it's a good practice to call it if it exists
            // to ensure proper cleanup of the extension part
            // and any other resources it may hold
            // (if applicable in your context)
            base.Dispose(isDisposing);
        }
           
    }
}

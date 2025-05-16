using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Msvc.Info.Server;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info.Cmd
{
    /// <summary>
    /// Command to start the MCP HTTP server
    /// </summary>
    [VisualStudioContribution]
    internal class StartMCPHttpServerCommand : Command
    {
        private readonly ILogger<StartMCPHttpServerCommand> _logger;
        private IHttpServer? _httpServer = null;
        private readonly IServiceProvider _sp;

        public StartMCPHttpServerCommand(ILogger<StartMCPHttpServerCommand> logger, IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%StartMCPHttpServer%")
        {
            Icon = new("KnownMonikers.Run", IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu]
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            if (_httpServer == null)
            {
                _httpServer = _sp.GetService<IHttpServer>();
                if (_httpServer == null)
                {
                    await Extensibility.Shell().ShowPromptAsync(
                        "MCP HTTP server is not available",
                        PromptOptions.OK,
                        cancellationToken);
                    return;
                }

                if (!_httpServer.HasMCPService)
                { // Check if the MCP service is available
                    await Extensibility.Shell().ShowPromptAsync(
                        "MCP HTTP server is not available. Please check logs for errors.",
                        PromptOptions.OK,
                        cancellationToken);
                    return;
                }
            }

            _logger?.LogInformation("Starting MCP HTTP server...");

            try
            {
                if (_httpServer.IsRunning)
                {
                    await Extensibility.Shell().ShowPromptAsync(
                        $"MCP HTTP server is already running at {_httpServer.BaseUrl}",
                        PromptOptions.OK,
                        cancellationToken);
                }
                else
                {
                    _httpServer.Start();
                    _logger?.LogInformation("MCP HTTP server started at {BaseUrl}", _httpServer.BaseUrl);
                    
                    await Extensibility.Shell().ShowPromptAsync(
                        $"MCP HTTP server started successfully at {_httpServer.BaseUrl}",
                        PromptOptions.OK,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start MCP HTTP server");
                await Extensibility.Shell().ShowPromptAsync(
                    $"Failed to start MCP HTTP server:\n{ex.Message}",
                    PromptOptions.OK,
                    cancellationToken);
            }
        }
    }
}

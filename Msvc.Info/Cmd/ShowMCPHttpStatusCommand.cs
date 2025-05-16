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
    /// Command to show the MCP HTTP server status
    /// </summary>
    [VisualStudioContribution]
    internal class ShowMCPHttpStatusCommand : Command
    {
        private readonly ILogger<ShowMCPHttpStatusCommand> _logger;
        private IHttpServer? _httpServer = null;
        private readonly IServiceProvider _sp;

        public ShowMCPHttpStatusCommand(ILogger<ShowMCPHttpStatusCommand> logger, IServiceProvider sp)
        {
            _logger = logger;
            _sp = sp;
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%ShowMCPHttpStatus%")
        {
            Icon = new("KnownMonikers.WebStatus", IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu]
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            if (_httpServer == null)
            {
                _httpServer = _sp.GetService<IHttpServer>();
                if(_httpServer == null)
                {
                    await Extensibility.Shell().ShowPromptAsync(
                        "MCP HTTP server is not available",
                        PromptOptions.OK,
                        cancellationToken);
                    return;
                }

                if (!_httpServer.HasMCPService) { // Check if the MCP service is available
                    await Extensibility.Shell().ShowPromptAsync(
                        "MCP HTTP server is not available. Please check logs for errors.",
                        PromptOptions.OK,
                        cancellationToken);
                    return;
                }
            }

            _logger?.LogInformation("Checking MCP HTTP server status...");

            string message;
            if (_httpServer.IsRunning)
            {
                message = $"MCP HTTP server is running at {_httpServer.BaseUrl}";
                _logger?.LogInformation(message);
            }
            else
            {
                message = "MCP HTTP server is not running";
                _logger?.LogInformation(message);
            }

            await Extensibility.Shell().ShowPromptAsync(
                message,
                PromptOptions.OK,
                cancellationToken);
        }
    }
}

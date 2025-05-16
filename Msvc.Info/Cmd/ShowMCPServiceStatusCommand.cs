using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Msvc.Info.Server;

namespace Msvc.Info.Cmd
{
    /// <summary>
    /// Command to show MCP Service status
    /// </summary>
    [VisualStudioContribution]
    internal class ShowMCPServiceStatusCommand : Command
    {
        private ILogger<ShowMCPServiceStatusCommand>? _logger; 

        public ShowMCPServiceStatusCommand(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetService<ILogger<ShowMCPServiceStatusCommand>>()
                      ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%ShowMCPServiceStatus%")
        {
            Icon = new("KnownMonikers.StatusInformation", IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu]
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Checking MCP Service status...");

            try
            {
                // Use the extension's service broker
                using var mcpService = await this.Extensibility.ServiceBroker.GetProxyAsync<IMCPService>(
                    IMCPService.Configuration.ServiceDescriptor,
                    cancellationToken);

                if (mcpService != null)
                {
                    try
                    {
                        var initResult = await mcpService.InitializeAsync(new { }, cancellationToken);
                        _logger?.LogInformation("MCP Service initialized successfully");

                        await Extensibility.Shell().ShowPromptAsync(
                            $"MCP Service is running successfully!\n\n" +
                            $"Service: {IMCPService.Configuration.ServiceName}\n" +
                            $"Version: {IMCPService.Configuration.ServiceVersion}\n\n" +
                            $"You can now connect external MCP clients using Visual Studio's Service Broker.",
                            PromptOptions.OK,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Connected to service but failed to initialize");
                        await Extensibility.Shell().ShowPromptAsync(
                            $"MCP Service is registered but failed to initialize:\n{ex.Message}",
                            PromptOptions.OK,
                            cancellationToken);
                    }
                    finally
                    {
                        (mcpService as IDisposable)?.Dispose();
                    }
                }
                else
                {
                    await Extensibility.Shell().ShowPromptAsync(
                        "MCP Service is not available. Please check logs for errors.",
                        PromptOptions.OK,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to check MCP Service status");
                await Extensibility.Shell().ShowPromptAsync(
                    $"Failed to check MCP Service status:\n{ex.Message}",
                    PromptOptions.OK,
                    cancellationToken);
            }
        }
    }
}

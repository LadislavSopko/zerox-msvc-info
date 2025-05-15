using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Shell;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info
{
    /// <summary>
    /// Command to test the MCP Service Broker
    /// </summary>
    [VisualStudioContribution]
    internal class TestMCPServiceCommand : Command
    {
        private ILogger<ShowMCPServiceStatusCommand>? _logger;

        public TestMCPServiceCommand(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetService<ILogger<ShowMCPServiceStatusCommand>>()
                      ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%TestMCPService%")
        {
            Icon = new("KnownMonikers.Test", IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu]
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Testing MCP Service Broker...");

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // Get service broker
                var serviceBrokerContainer = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                var serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();

                if (serviceBroker == null)
                {
                    await this.Extensibility.Shell().ShowPromptAsync(
                        "Service Broker not available",
                        PromptOptions.OK,
                        cancellationToken);
                    return;
                }

                // Connect to MCP service via Service Broker's proxy
                using var mcpService = await serviceBroker.GetProxyAsync<IMCPService>(
                    MCPServiceDescriptor.Descriptor,
                    cancellationToken: cancellationToken);

                if (mcpService != null)
                {
                    // Test initialize
                    var initResult = await mcpService.InitializeAsync(new { }, cancellationToken);
                    _logger?.LogInformation("Initialize result: {Result}", JsonSerializer.Serialize(initResult));

                    // Test list tools
                    var toolsResult = await mcpService.ListToolsAsync(cancellationToken);
                    _logger?.LogInformation("Tools result: {Result}", JsonSerializer.Serialize(toolsResult));

                    // Test get solution projects
                    var projectsArgs = JsonSerializer.SerializeToElement(new { });
                    var projectsResult = await mcpService.CallToolAsync("get_solution_projects", projectsArgs, cancellationToken);
                    _logger?.LogInformation("Projects result: {Result}", JsonSerializer.Serialize(projectsResult));

                    await this.Extensibility.Shell().ShowPromptAsync(
                        $"MCP Service Broker test completed successfully!\n\n" +
                        $"Check the output window for detailed results.",
                        PromptOptions.OK,
                        cancellationToken);
                }
                else
                {
                    await this.Extensibility.Shell().ShowPromptAsync(
                        "MCP Service is not available. Please check logs for errors.",
                        PromptOptions.OK,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MCP Service Broker test failed");
                await this.Extensibility.Shell().ShowPromptAsync(
                    $"MCP Service Broker test failed:\n{ex.Message}",
                    PromptOptions.OK,
                    cancellationToken);
            }
        }
    }

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
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                
                // Get service broker
                var serviceBrokerContainer = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                var serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();
                
                if (serviceBroker == null)
                {
                    await this.Extensibility.Shell().ShowPromptAsync(
                        "Service Broker not available", 
                        PromptOptions.OK,
                        cancellationToken);
                    return;
                }


                // Try to connect and get info
                try
                {
                    using var mcpService = await serviceBroker.GetProxyAsync<IMCPService>(
                        MCPServiceDescriptor.Descriptor,
                        cancellationToken: cancellationToken);

                    if (mcpService != null)
                    {
                        var initResult = await mcpService.InitializeAsync(new { }, cancellationToken);

                        await this.Extensibility.Shell().ShowPromptAsync(
                            $"MCP Service Broker is running successfully!\n\n" +
                            $"Service Moniker: {MCPServiceDescriptor.Moniker}\n" +
                            $"Version: {MCPServiceDescriptor.Moniker.Version}\n\n" +
                            $"You can now connect external MCP clients using Visual Studio's Service Broker.",
                            PromptOptions.OK,
                            cancellationToken);
                    }
                    else
                    {
                        await this.Extensibility.Shell().ShowPromptAsync(
                            "MCP Service is not available. Please check logs for errors.",
                            PromptOptions.OK,
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Connected to service but failed to initialize");
                    await this.Extensibility.Shell().ShowPromptAsync(
                        $"MCP Service is registered but failed to initialize:\n{ex.Message}",
                        PromptOptions.OK,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to check MCP Service status");
                await this.Extensibility.Shell().ShowPromptAsync(
                    $"Failed to check MCP Service status:\n{ex.Message}",
                    PromptOptions.OK,
                    cancellationToken);
            }
        }
    }
}
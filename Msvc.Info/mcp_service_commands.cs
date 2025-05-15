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
        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("Test MCP Service")
        {
            Icon = new("KnownMonikers.Test", IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu]
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            var logger = this.Extensibility.ServiceCollection.GetService<ILogger<TestMCPServiceCommand>>();
            logger?.LogInformation("Testing MCP Service Broker...");

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                
                // Get service broker
                var serviceBrokerContainer = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                var serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();
                
                if (serviceBroker == null)
                {
                    await context.ShowPromptAsync("Service Broker not available", PromptOptions.OK, cancellationToken);
                    return;
                }

                // Connect to MCP service
                using var mcpProxy = await MCPServiceProxy.CreateAsync(serviceBroker, cancellationToken);

                // Test initialize
                var initResult = await mcpProxy.InitializeAsync(new { }, cancellationToken);
                logger?.LogInformation("Initialize result: {Result}", JsonSerializer.Serialize(initResult));

                // Test list tools
                var toolsResult = await mcpProxy.ListToolsAsync(cancellationToken);
                logger?.LogInformation("Tools result: {Result}", JsonSerializer.Serialize(toolsResult));

                // Test get solution projects
                var projectsArgs = JsonSerializer.SerializeToElement(new { });
                var projectsResult = await mcpProxy.CallToolAsync("get_solution_projects", projectsArgs, cancellationToken);
                logger?.LogInformation("Projects result: {Result}", JsonSerializer.Serialize(projectsResult));

                await context.ShowPromptAsync(
                    $"MCP Service Broker test completed successfully!\n\n" +
                    $"Check the output window for detailed results.",
                    PromptOptions.OK,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "MCP Service Broker test failed");
                await context.ShowPromptAsync(
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
        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("Show MCP Service Status")
        {
            Icon = new("KnownMonikers.StatusInformation", IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu]
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            var logger = this.Extensibility.ServiceCollection.GetService<ILogger<ShowMCPServiceStatusCommand>>();
            logger?.LogInformation("Checking MCP Service status...");

            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                
                // Get service broker
                var serviceBrokerContainer = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                var serviceBroker = serviceBrokerContainer?.GetFullAccessServiceBroker();
                
                if (serviceBroker == null)
                {
                    await context.ShowPromptAsync("Service Broker not available", PromptOptions.OK, cancellationToken);
                    return;
                }

                // Check if service is available
                var availableServices = await serviceBroker.GetAvailableServicesAsync(cancellationToken);
                var mcpServiceAvailable = availableServices.Any(s => s.Equals(MCPServiceBrokerDescriptor.Moniker));

                if (mcpServiceAvailable)
                {
                    // Try to connect and get info
                    try
                    {
                        using var mcpProxy = await MCPServiceProxy.CreateAsync(serviceBroker, cancellationToken);
                        var initResult = await mcpProxy.InitializeAsync(new { }, cancellationToken);
                        
                        await context.ShowPromptAsync(
                            $"MCP Service Broker is running successfully!\n\n" +
                            $"Service Moniker: {MCPServiceBrokerDescriptor.Moniker}\n" +
                            $"Version: {MCPServiceBrokerDescriptor.Moniker.Version}\n\n" +
                            $"You can now connect external MCP clients using Visual Studio's Service Broker.",
                            PromptOptions.OK,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Connected to service but failed to initialize");
                        await context.ShowPromptAsync(
                            $"MCP Service is registered but failed to initialize:\n{ex.Message}",
                            PromptOptions.OK,
                            cancellationToken);
                    }
                }
                else
                {
                    await context.ShowPromptAsync(
                        "MCP Service is not available. Please check logs for errors.",
                        PromptOptions.OK,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to check MCP Service status");
                await context.ShowPromptAsync(
                    $"Failed to check MCP Service status:\n{ex.Message}",
                    PromptOptions.OK,
                    cancellationToken);
            }
        }
    }
}
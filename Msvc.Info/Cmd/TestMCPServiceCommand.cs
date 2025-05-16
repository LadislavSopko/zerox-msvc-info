

using Microsoft.Extensions.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Msvc.Info.Server;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info.Cmd
{
    /// <summary>
    /// Command to test the MCP Service
    /// </summary>
    [VisualStudioContribution]
    internal class TestMCPServiceCommand : Command
    {
        private ILogger<TestMCPServiceCommand>? _logger;

        public TestMCPServiceCommand(ILogger<TestMCPServiceCommand> logger)
        {
            _logger = logger;
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
            _logger?.LogInformation("Testing MCP Service...");

           
            // Use the extension's service broker
            var mcpService = await this.Extensibility.ServiceBroker.GetProxyAsync<IMCPService>(
                IMCPService.Configuration.ServiceDescriptor,
                cancellationToken);
            try
            {
                if (mcpService != null)
                {
                    try
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

                        await Extensibility.Shell().ShowPromptAsync(
                            $"MCP Service test completed successfully!\n\n" +
                            $"Check the output window for detailed results.",
                            PromptOptions.OK,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "MCP Service test failed during operation");
                        await Extensibility.Shell().ShowPromptAsync(
                            $"MCP Service test failed:\n{ex.Message}",
                            PromptOptions.OK,
                            cancellationToken);
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
            finally
            {
                (mcpService as IDisposable)?.Dispose();
            }

        }
    }
}

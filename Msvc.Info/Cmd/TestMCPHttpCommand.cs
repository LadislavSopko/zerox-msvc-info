using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info.Cmd
{
    /// <summary>
    /// Command to test the MCP HTTP endpoint
    /// </summary>
    [VisualStudioContribution]
    internal class TestMCPHttpCommand : Command
    {
        private readonly ILogger<TestMCPHttpCommand> _logger;
        private readonly HttpClient _httpClient;

        public TestMCPHttpCommand(ILogger<TestMCPHttpCommand> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <inheritdoc />
        public override CommandConfiguration CommandConfiguration => new("%TestMCPHttp%")
        {
            Icon = new("KnownMonikers.Web", IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ToolsMenu]
        };

        /// <inheritdoc />
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Testing MCP HTTP endpoint...");

            try
            {
                var baseUrl = "http://localhost:3000/jsonrpc";

                // Test initialize
                var initRequest = CreateJsonRpcRequest("initialize", new { }, 1);
                var initResponse = await SendRequestAsync(baseUrl, initRequest, cancellationToken);
                _logger?.LogInformation("Initialize response: {Response}", initResponse);

                // Test list tools
                var toolsRequest = CreateJsonRpcRequest("tools/list", new { }, 2);
                var toolsResponse = await SendRequestAsync(baseUrl, toolsRequest, cancellationToken);
                _logger?.LogInformation("Tools response: {Response}", toolsResponse);

                // Test get solution projects
                var projectsRequest = CreateJsonRpcRequest("tools/call", new { name = "get_solution_projects", arguments = new { } }, 3);
                var projectsResponse = await SendRequestAsync(baseUrl, projectsRequest, cancellationToken);
                _logger?.LogInformation("Projects response: {Response}", projectsResponse);

                await Extensibility.Shell().ShowPromptAsync(
                    $"MCP HTTP test completed successfully!\n\n" +
                    $"Endpoint: {baseUrl}\n" +
                    $"Check the output window for detailed results.",
                    PromptOptions.OK,
                    cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "HTTP request failed");
                await Extensibility.Shell().ShowPromptAsync(
                    $"MCP HTTP test failed:\n{ex.Message}\n\nMake sure the HTTP server is running on port 3000.",
                    PromptOptions.OK,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "MCP HTTP test failed");
                await Extensibility.Shell().ShowPromptAsync(
                    $"MCP HTTP test failed:\n{ex.Message}",
                    PromptOptions.OK,
                    cancellationToken);
            }
        }

        private object CreateJsonRpcRequest(string method, object parameters, int id)
        {
            return new
            {
                jsonrpc = "2.0",
                method = method,
                @params = parameters,
                id = id
            };
        }

        private async Task<string> SendRequestAsync(string url, object request, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
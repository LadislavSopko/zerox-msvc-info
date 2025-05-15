using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.ServiceHub.Framework.Services;
using StreamJsonRpc;
using System.Collections.Immutable;

namespace Msvc.Info
{
    /// <summary>
    /// Service descriptor for the MCP Service
    /// </summary>
    internal class MCPServiceBrokerDescriptor
    {
        /// <summary>
        /// The moniker for the MCP service
        /// </summary>
        public static readonly ServiceMoniker Moniker = new ServiceMoniker("Microsoft.VisualStudio.MCP", new Version(1, 0));
        
        /// <summary>
        /// Service descriptor with moniker and visibility
        /// </summary>
        public static readonly ServiceRpcDescriptor Descriptor = new ServiceRpcDescriptor(
            Moniker, 
            clientInterface: null,
            ServiceRpcDescriptor.Formatters.MessagePack,
            ServiceRpcDescriptor.MessageDelimiters.HttpLikeHeaders)
        {
            // Allow access from extensions and external processes
            Visibility = ServiceAudience.AllClientsIncludingGuests,
            AllowGuestClients = true
        };
    }

    /// <summary>
    /// Factory for creating MCP service instances
    /// </summary>
    internal class MCPServiceFactory : IServiceFactory
    {
        private readonly IMCPService _mcpService;

        public MCPServiceFactory(IMCPService mcpService)
        {
            _mcpService = mcpService;
        }

        public Task<object?> CreateAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions options, IServiceBroker serviceBroker, AuthorizationServiceClient? authorizationServiceClient)
        {
            // Return our singleton MCP service instance
            return Task.FromResult<object?>(_mcpService);
        }
    }

    /// <summary>
    /// Local RPC target for the MCP service (handles actual JSON-RPC calls)
    /// </summary>
    internal class MCPServiceRpcTarget
    {
        private readonly IMCPService _mcpService;

        public MCPServiceRpcTarget(IMCPService mcpService)
        {
            _mcpService = mcpService;
        }

        // MCP Protocol Methods (these will be exposed as JSON-RPC methods)
        public async Task<object> Initialize(object parameters, CancellationToken cancellationToken = default)
        {
            return await _mcpService.InitializeAsync(parameters, cancellationToken);
        }

        [JsonRpcMethod("tools/list")]
        public async Task<object> ToolsList(CancellationToken cancellationToken = default)
        {
            return await _mcpService.ListToolsAsync(cancellationToken);
        }

        [JsonRpcMethod("tools/call")]
        public async Task<object> ToolsCall(string name, System.Text.Json.JsonElement arguments, CancellationToken cancellationToken = default)
        {
            return await _mcpService.CallToolAsync(name, arguments, cancellationToken);
        }

        [JsonRpcMethod("resources/list")]
        public async Task<object> ResourcesList(CancellationToken cancellationToken = default)
        {
            return await _mcpService.ListResourcesAsync(cancellationToken);
        }

        [JsonRpcMethod("resources/read")]
        public async Task<object> ResourcesRead(string uri, CancellationToken cancellationToken = default)
        {
            return await _mcpService.ReadResourceAsync(uri, cancellationToken);
        }
    }

    /// <summary>
    /// Service Broker pipeline for creating the MCP service with JSON-RPC
    /// </summary>
    internal static class MCPServiceBrokerPipeline
    {
        public static ServiceRpcDescriptor CreateDescriptor()
        {
            return MCPServiceBrokerDescriptor.Descriptor
                .WithExceptionStrategy(StreamJsonRpc.ExceptionStrategy.CommonErrorData);
        }

        public static IReadOnlyDictionary<string, object> CreateConnectionMetadata()
        {
            return new Dictionary<string, object>
            {
                ["protocol"] = "mcp",
                ["version"] = "1.0"
            };
        }
    }

    /// <summary>
    /// Simplified proxy for connecting to the MCP service
    /// </summary>
    public class MCPServiceProxy : IDisposable
    {
        private readonly IServiceBroker _serviceBroker;
        private readonly IDisposable _serviceConnection;
        private readonly JsonRpc _jsonRpc;

        public static async Task<MCPServiceProxy> CreateAsync(IServiceBroker serviceBroker, CancellationToken cancellationToken = default)
        {
            // Get a pipe to the service
            var pipe = await serviceBroker.GetPipeAsync(
                MCPServiceBrokerDescriptor.Moniker,
                cancellationToken: cancellationToken);

            // Create JSON-RPC connection
            var jsonRpc = JsonRpc.Attach(pipe.Item1);
            
            return new MCPServiceProxy(serviceBroker, pipe.Item2, jsonRpc);
        }

        private MCPServiceProxy(IServiceBroker serviceBroker, IDisposable serviceConnection, JsonRpc jsonRpc)
        {
            _serviceBroker = serviceBroker;
            _serviceConnection = serviceConnection;
            _jsonRpc = jsonRpc;
        }

        public JsonRpc JsonRpc => _jsonRpc;

        // Convenient methods for MCP operations
        public async Task<object> InitializeAsync(object parameters, CancellationToken cancellationToken = default)
        {
            return await _jsonRpc.InvokeAsync<object>("Initialize", new object[] { parameters }, cancellationToken);
        }

        public async Task<object> ListToolsAsync(CancellationToken cancellationToken = default)
        {
            return await _jsonRpc.InvokeAsync<object>("tools/list", cancellationToken);
        }

        public async Task<object> CallToolAsync(string name, System.Text.Json.JsonElement arguments, CancellationToken cancellationToken = default)
        {
            return await _jsonRpc.InvokeAsync<object>("tools/call", new object[] { name, arguments }, cancellationToken);
        }

        public async Task<object> ListResourcesAsync(CancellationToken cancellationToken = default)
        {
            return await _jsonRpc.InvokeAsync<object>("resources/list", cancellationToken);
        }

        public async Task<object> ReadResourceAsync(string uri, CancellationToken cancellationToken = default)
        {
            return await _jsonRpc.InvokeAsync<object>("resources/read", new object[] { uri }, cancellationToken);
        }

        public void Dispose()
        {
            _jsonRpc?.Dispose();
            _serviceConnection?.Dispose();
        }
    }
}
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.ServiceHub.Framework.Services;
using StreamJsonRpc;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Extensibility;

namespace Msvc.Info
{
    /// <summary>
    /// Service descriptor for the MCP Service
    /// </summary>
    internal static class MCPServiceDescriptor
    {
        /// <summary>
        /// The moniker for the MCP service
        /// </summary>
        public static readonly ServiceMoniker Moniker = new ServiceMoniker("Microsoft.VisualStudio.MCP", new Version(1, 0));
        
        /// <summary>
        /// Service descriptor with moniker and visibility
        /// </summary>
        public static readonly ServiceJsonRpcDescriptor Descriptor = new ServiceJsonRpcDescriptor(
            Moniker, 
            ServiceJsonRpcDescriptor.Formatters.UTF8,
            ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);
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

        public Task<object?> CreateAsync(ServiceActivationOptions options, CancellationToken cancellationToken)
        {
            // Return our singleton MCP service instance
            return Task.FromResult<object?>(_mcpService);
        }

        public Task<object> CreateAsync(ServiceMoniker serviceMoniker, ServiceActivationOptions activationOptions, IServiceProvider serviceProvider, AuthorizationServiceClient authorizationServiceClient, Type? instanceType, Type? interfaceType, CancellationToken cancellationToken)
        {
            // Return our singleton MCP service instance
            return Task.FromResult<object>(_mcpService);
        }

        public ServiceRpcDescriptor GetServiceDescriptor(ServiceMoniker serviceMoniker)
        {
            // Return the service descriptor
            return MCPServiceDescriptor.Descriptor;
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
        public static ServiceJsonRpcDescriptor CreateDescriptor()
        {
            return MCPServiceDescriptor.Descriptor
                .WithExceptionStrategy(ExceptionProcessing.CommonErrorData);
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

}
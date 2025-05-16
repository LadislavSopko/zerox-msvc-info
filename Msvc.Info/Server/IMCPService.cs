using Microsoft.ServiceHub.Framework;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info.Server
{
    /// <summary>
    /// MCP Service interface for JSON-RPC communication
    /// </summary>
    public interface IMCPService : IDisposable
    {
        /// <summary>
        /// Initialize the MCP service
        /// </summary>
        Task<object> InitializeAsync(object parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// List available tools
        /// </summary>
        Task<object> ListToolsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Call a specific tool
        /// </summary>
        Task<object> CallToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default);

        /// <summary>
        /// List available resources
        /// </summary>
        Task<object> ListResourcesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Read a specific resource
        /// </summary>
        Task<object> ReadResourceAsync(string uri, CancellationToken cancellationToken = default);

        /// <summary>
        /// Service configuration
        /// </summary>
        public static class Configuration
        {
            public const string ServiceName = "Microsoft.VisualStudio.MCP";
            public static readonly Version ServiceVersion = new(1, 0);

            public static readonly ServiceMoniker ServiceMoniker = new(ServiceName, ServiceVersion);

            public static ServiceRpcDescriptor ServiceDescriptor => new ServiceJsonRpcDescriptor(
                ServiceMoniker,
                ServiceJsonRpcDescriptor.Formatters.UTF8,
                ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);
        }
    }
}

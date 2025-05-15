using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.Logging;
using Zerox.Info.Logging;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using System.Threading;
using StreamJsonRpc;
using System.IO.Pipelines;
using Zerox.Info.Core.Services;
using Microsoft.ServiceHub.Framework.Services;
using Nerdbank.Streams;

namespace Msvc.Info
{
    /// <summary>
    /// Extension entrypoint for the VisualStudio.Extensibility extension.
    /// </summary>
    [VisualStudioContribution]
    internal class ExtensionEntrypoint : Extension
    {
        
        /// <inheritdoc />
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            RequiresInProcessHosting = true,
        };

        /// <inheritdoc />
        protected override void InitializeServices(IServiceCollection serviceCollection)
        {
            base.InitializeServices(serviceCollection);

            // Configure logging
            serviceCollection.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddConsole(); // Optional: also log to console
            });

            // Register VS services using MEF imports
            serviceCollection.AddSingleton<IVsOutputWindow>(provider =>
            {
                return ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    return await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsOutputWindow, IVsOutputWindow>();
                });
            });

            // Register logger service (uses TraceSource and VS Output Window)
            serviceCollection.AddScoped<IExtensionLogger, ExtensionLogger>();

            // Register path translation service
            serviceCollection.AddSingleton<IPathTranslationService, PathTranslationService>();

            // Register MCP service implementation
            serviceCollection.AddSingleton<IMCPService, MCPServiceBrokerImpl>();

            // Register the startup component
            serviceCollection.AddSingleton<MCPServiceBrokerStartup>();

        }

        /// <summary>
        /// Service broker startup task to register MCP service
        /// </summary>
        [VisualStudioContribution]
        internal class MCPServiceBrokerStartup : ExtensionPart
        {
            private readonly IMCPService _mcpService;
            private readonly ILogger<MCPServiceBrokerStartup> _logger;

            public MCPServiceBrokerStartup(
                IMCPService mcpService,
                ILogger<MCPServiceBrokerStartup> logger)
            {
                _mcpService = mcpService;
                _logger = logger;
            }

            protected override async Task InitializeAsync(CancellationToken cancellationToken)
            {
                await base.InitializeAsync(cancellationToken);

                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                    // Get the global service broker container
                    var serviceBrokerContainer = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();

                    if (serviceBrokerContainer == null)
                    {
                        _logger.LogError("Could not get SVsBrokeredServiceContainer service");
                        return;
                    }

                    // Register our service directly with the service broker
                    serviceBrokerContainer.Proffer(
                        MCPServiceDescriptor.Descriptor,
                        (ServiceMoniker moniker, ServiceActivationOptions options, IServiceBroker broker, AuthorizationServiceClient auth, CancellationToken ct) =>
                        {
                            return new ValueTask<object?>(_mcpService);
                        });

                    _logger.LogInformation("MCP Service proffered with Service Broker at {Moniker}", MCPServiceDescriptor.Moniker);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to proffer MCP service with Service Broker");
                    throw;
                }
            }

        }
    }
}
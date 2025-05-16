using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using Msvc.Info.Core.Services;
using Msvc.Info.Logging;
using Msvc.Info.Server;

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

            // Register logger service (uses TraceSource and VS Output Window)
            serviceCollection.AddScoped<IExtensionLogger, ExtensionLogger>();

            // Register path translation service
            serviceCollection.AddSingleton<IPathTranslationService, PathTranslationService>();

            // Proffer the MCP service with Service Broker
            serviceCollection.ProfferBrokeredService(MCPService.BrokeredServiceConfiguration, IMCPService.Configuration.ServiceDescriptor);
        }
    }
}

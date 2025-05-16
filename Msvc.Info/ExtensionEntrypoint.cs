using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Extensibility;
using Msvc.Info.Core.Services;
using Msvc.Info.Logging;
using Msvc.Info.Server;
using System.Threading;
using System.Threading.Tasks;

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

        protected override Task OnInitializedAsync(VisualStudioExtensibility extensibility, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

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

            // Register HTTP server configuration
            serviceCollection.AddSingleton<MCPHttpServerConfiguration>();
            
            // Register HTTP server
            serviceCollection.AddSingleton<IHttpServer, MCPHttpServer>();

            // Proffer the MCP service with Service Broker
            serviceCollection.ProfferBrokeredService(MCPService.BrokeredServiceConfiguration, IMCPService.Configuration.ServiceDescriptor);
        }
    }
}

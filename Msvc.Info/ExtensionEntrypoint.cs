using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.Extensions.Logging;
using Zerox.Info.Logging;

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
        }
    }
}

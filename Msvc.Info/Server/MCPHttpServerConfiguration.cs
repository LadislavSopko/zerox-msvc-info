namespace Msvc.Info.Server
{
    /// <summary>
    /// Configuration for the MCP HTTP server
    /// </summary>
    public class MCPHttpServerConfiguration
    {
        /// <summary>
        /// Gets or sets whether the HTTP server is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the host to bind to
        /// </summary>
        public string Host { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the port to listen on
        /// </summary>
        public int Port { get; set; } = 3000;

        /// <summary>
        /// Gets the base URL for the HTTP server
        /// </summary>
        public string BaseUrl => $"http://{Host}:{Port}/";
    }
}
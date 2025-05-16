using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info.Server
{
    /// <summary>
    /// HTTP server that exposes the MCP service via JSON-RPC over HTTP
    /// </summary>
    public class MCPHttpServer : IHttpServer, IDisposable
    {
        internal IMCPService? _mcpService = null;
        private readonly ILogger<MCPHttpServer> _logger;
        private readonly MCPHttpServerConfiguration _configuration;
        private HttpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _disposed;
        
        public MCPHttpServer(ILogger<MCPHttpServer> logger, MCPHttpServerConfiguration? configuration = null)
        {
            //_mcpService = mcpService ?? throw new ArgumentNullException(nameof(mcpService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? new MCPHttpServerConfiguration();
        }

        public string BaseUrl => _configuration.BaseUrl;

        public bool IsRunning => _listener?.IsListening ?? false;

        public bool HasMCPService => _mcpService != null;

        public void Start()
        {
            if (_listener != null)
            {
                _logger.LogWarning("HTTP server is already running");
                return;
            }

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(_configuration.BaseUrl);
                _listener.Start();

                _cancellationTokenSource = new CancellationTokenSource();
                _listenerTask = AcceptRequestsAsync(_cancellationTokenSource.Token);

                _logger.LogInformation("MCP HTTP server started at {BaseUrl}", BaseUrl);
            }
            catch (HttpListenerException ex)
            {
                _logger.LogError(ex, "Failed to start HTTP server");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_listener == null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Stopping MCP HTTP server");
                
                _cancellationTokenSource?.Cancel();
                _listener.Stop();
                
                if (_listenerTask != null)
                {
                    await _listenerTask.ConfigureAwait(false);
                }
                
                _listener.Close();
                _listener = null;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _logger.LogInformation("MCP HTTP server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping HTTP server");
            }
        }

        private async Task AcceptRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null)
            {
                try
                {
                    var contextTask = _listener.GetContextAsync();
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.Token.Register(() => _listener?.Stop());

                    var context = await contextTask.ConfigureAwait(false);
                    
                    // Process request asynchronously
                    _ = Task.Run(async () => await ProcessRequestAsync(context, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // Listener was disposed, exit gracefully
                    break;
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
                {
                    // Operation was aborted, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting HTTP request");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                // Only accept POST requests to /jsonrpc
                if (context.Request.HttpMethod != "POST" || !context.Request.Url.AbsolutePath.Equals("/jsonrpc", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                    return;
                }

                // Read request body
                string requestBody;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                }

                _logger.LogDebug("Received JSON-RPC request: {RequestBody}", requestBody);

                // Parse JSON-RPC request
                JsonDocument jsonDoc;
                try
                {
                    jsonDoc = JsonDocument.Parse(requestBody);
                }
                catch (JsonException ex)
                {
                    await SendErrorResponseAsync(context, -32700, "Parse error", null);
                    _logger.LogError(ex, "Failed to parse JSON request");
                    return;
                }

                using (jsonDoc)
                {
                    var root = jsonDoc.RootElement;
                    
                    // Validate JSON-RPC format
                    if (!root.TryGetProperty("jsonrpc", out var jsonrpcProp) || jsonrpcProp.GetString() != "2.0")
                    {
                        await SendErrorResponseAsync(context, -32600, "Invalid Request", null);
                        return;
                    }

                    if (!root.TryGetProperty("method", out var methodProp) || methodProp.ValueKind != JsonValueKind.String)
                    {
                        await SendErrorResponseAsync(context, -32600, "Invalid Request", null);
                        return;
                    }

                    var method = methodProp.GetString();
                    var id = root.TryGetProperty("id", out var idProp) ? idProp : (JsonElement?)null;
                    var parameters = root.TryGetProperty("params", out var paramsProp) ? paramsProp : JsonDocument.Parse("{}").RootElement;

                    // Execute method
                    try
                    {
                        object result = method switch
                        {
                            "initialize" => await _mcpService.InitializeAsync(parameters, cancellationToken),
                            "tools/list" => await _mcpService.ListToolsAsync(cancellationToken),
                            "tools/call" => await CallToolAsync(parameters, cancellationToken),
                            "resources/list" => await _mcpService.ListResourcesAsync(cancellationToken),
                            "resources/read" => await ReadResourceAsync(parameters, cancellationToken),
                            _ => throw new InvalidOperationException($"Method not found: {method}")
                        };

                        // Send success response
                        await SendSuccessResponseAsync(context, result, id);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.StartsWith("Method not found"))
                    {
                        await SendErrorResponseAsync(context, -32601, "Method not found", id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing method: {Method}", method);
                        await SendErrorResponseAsync(context, -32603, "Internal error", id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing HTTP request");
                try
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Close();
                }
                catch { }
            }
        }

        private async Task<object> CallToolAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            if (!parameters.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("Missing or invalid 'name' parameter");
            }

            var toolName = nameProp.GetString();
            var arguments = parameters.TryGetProperty("arguments", out var argsProp) ? argsProp : JsonDocument.Parse("{}").RootElement;

            return await _mcpService.CallToolAsync(toolName!, arguments, cancellationToken);
        }

        private async Task<object> ReadResourceAsync(JsonElement parameters, CancellationToken cancellationToken)
        {
            if (!parameters.TryGetProperty("uri", out var uriProp) || uriProp.ValueKind != JsonValueKind.String)
            {
                throw new ArgumentException("Missing or invalid 'uri' parameter");
            }

            var uri = uriProp.GetString();
            return await _mcpService.ReadResourceAsync(uri!, cancellationToken);
        }

        private async Task SendSuccessResponseAsync(HttpListenerContext context, object result, JsonElement? id)
        {
            var response = new
            {
                jsonrpc = "2.0",
                result = result,
                id = id
            };

            await SendJsonResponseAsync(context, response);
        }

        private async Task SendErrorResponseAsync(HttpListenerContext context, int code, string message, JsonElement? id)
        {
            var response = new
            {
                jsonrpc = "2.0",
                error = new
                {
                    code = code,
                    message = message
                },
                id = id
            };

            await SendJsonResponseAsync(context, response);
        }

        private async Task SendJsonResponseAsync(HttpListenerContext context, object response)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
            var buffer = Encoding.UTF8.GetBytes(json);

            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();

            _logger.LogDebug("Sent JSON-RPC response: {Response}", json);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            try
            {
                StopAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }
        }
    }
}

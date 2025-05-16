# HTTP Transport Implementation Plan

## Overview

Implement HTTP transport layer for the MCP service to enable external access beyond VS Service Broker.

## Architecture

- Use HttpListener (built into .NET Framework 4.8)
- Forward JSON-RPC 2.0 requests to existing MCPService
- No authentication required initially (per MCP standard practice)
- Simple request/response model

## Implementation Steps

### 1. Create HTTP Server Component

```csharp
public class MCPHttpServer
{
    private HttpListener _listener;
    private IMCPService _mcpService;
    private bool _isRunning;
    
    // Start server on configured port
    // Handle incoming HTTP POST requests
    // Parse JSON-RPC requests
    // Route to appropriate MCPService method
    // Return JSON-RPC response
}
```

### 2. Request Handler

- Accept POST requests at `/jsonrpc` endpoint
- Parse JSON-RPC request body
- Map method names to MCPService methods:
  - `initialize` → `InitializeAsync`
  - `tools/list` → `ListToolsAsync`
  - `tools/call` → `CallToolAsync`
  - `resources/list` → `ListResourcesAsync`
  - `resources/read` → `ReadResourceAsync`

### 3. Configuration

```json
{
  "httpServer": {
    "enabled": true,
    "port": 3000,
    "host": "localhost"
  }
}
```

### 4. Lifecycle Management

- Start HTTP server when extension loads
- Stop server on extension shutdown
- Handle port conflicts gracefully
- Log server status

### 5. Error Handling

- Return proper JSON-RPC error responses
- Log exceptions without exposing internals
- Handle connection timeouts

## Testing Plan

1. Create simple HTTP client test
2. Test with curl/Postman
3. Verify JSON-RPC compliance
4. Test error scenarios

## Example Usage

```bash
# Initialize
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "initialize", "params": {}, "id": 1}'

# List tools
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "tools/list", "params": {}, "id": 2}'
```

## Future Enhancements

- Add API key authentication
- Implement SSE for streaming updates
- Add CORS support for web clients
- Rate limiting
- HTTPS support
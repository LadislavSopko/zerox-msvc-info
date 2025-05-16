# MCP HTTP Endpoint Usage

The MCP service is now exposed via HTTP on `http://localhost:3000/jsonrpc`

## Testing with curl

```bash
# Initialize the service
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "initialize", "params": {}, "id": 1}'

# List available tools
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "tools/list", "params": {}, "id": 2}'

# Get solution projects
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "tools/call", "params": {"name": "get_solution_projects", "arguments": {}}, "id": 3}'

# List resources
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "resources/list", "params": {}, "id": 4}'

# Read a resource (replace with actual resource URI)
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "resources/read", "params": {"uri": "vs://solution"}, "id": 5}'
```

## Configuration

The HTTP server runs on port 3000 by default. You can modify the configuration in `MCPHttpServerConfiguration.cs`:

- `Enabled`: Whether the HTTP server should start (default: true)
- `Host`: The host to bind to (default: "localhost")
- `Port`: The port to listen on (default: 3000)

## Error Handling

The server returns standard JSON-RPC 2.0 error responses:

- `-32700`: Parse error
- `-32600`: Invalid Request
- `-32601`: Method not found
- `-32603`: Internal error

## Security Note

Currently, the HTTP endpoint has no authentication. It should only be used for local development. For production use, consider adding:

- API key authentication
- HTTPS support
- IP whitelisting
- Rate limiting
# MSVC MCP Server HTTP Usage Guide

The MSVC MCP Server provides an HTTP endpoint for external tools to access Visual Studio project information using the Model Context Protocol.

## Quick Start

1. **Start the HTTP Server**
   - In Visual Studio, go to **Tools** → **Start MCP HTTP Server**
   - Server starts on `http://localhost:3000/jsonrpc`
   - Check status with **Tools** → **Show MCP HTTP Status**

2. **Test the Connection**
   - Use **Tools** → **Test MCP HTTP** for automatic testing
   - Or use curl commands below

## Example Commands

### Initialize Session
```bash
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "initialize", "params": {}, "id": 1}'
```

### List Available Tools
```bash
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "tools/list", "params": {}, "id": 2}'
```

### Get Solution Projects
```bash
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "tools/call", "params": {"name": "get_solution_projects", "arguments": {}}, "id": 3}'
```

### Get Document Outline
```bash
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0", 
    "method": "tools/call", 
    "params": {
      "name": "get_document_outline", 
      "arguments": {"filePath": "C:\\Path\\To\\File.cs"}
    }, 
    "id": 4
  }'
```

### Find Symbols
```bash
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0", 
    "method": "tools/call", 
    "params": {
      "name": "find_symbols", 
      "arguments": {"name": "MyClass"}
    }, 
    "id": 5
  }'
```

### Translate Path (Windows to WSL)
```bash
curl -X POST http://localhost:3000/jsonrpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0", 
    "method": "tools/call", 
    "params": {
      "name": "translate_path", 
      "arguments": {
        "path": "C:\\Projects\\MyApp", 
        "sourceFormat": "Windows", 
        "targetFormat": "WSL"
      }
    }, 
    "id": 6
  }'
```

## Available MCP Methods

| Method | Description |
|--------|-------------|
| `initialize` | Initialize MCP session |
| `tools/list` | List available tools |
| `tools/call` | Call a specific tool |
| `resources/list` | List available resources |
| `resources/read` | Read a specific resource |

## Available Tools

| Tool Name | Description | Required Arguments |
|-----------|-------------|-------------------|
| `get_solution_projects` | Get all projects in the solution | None |
| `find_symbols` | Find symbols by name | `name` (string) |
| `get_symbol_at_location` | Get symbol at specific location | `filePath`, `line`, `column` |
| `get_document_outline` | Get document symbols/outline | `filePath` (string) |
| `translate_path` | Convert paths between formats | `path`, `sourceFormat`, `targetFormat` |

## Response Format

Success response:
```json
{
  "jsonrpc": "2.0",
  "result": {
    // Method-specific result data
  },
  "id": 1
}
```

Error response:
```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32601,
    "message": "Method not found"
  },
  "id": 1
}
```

## Integration with MCP Clients

To use with Claude Desktop or other MCP clients:

1. Configure the client to connect to `http://localhost:3000/jsonrpc`
2. Start the HTTP server in Visual Studio
3. The client will automatically discover available tools

## Troubleshooting

- **Server not responding**: Check if the server is running with the status command
- **Method not found**: Verify the method name and parameters
- **Path issues**: Use the path translation tool to convert between Windows/WSL formats
- **Check logs**: Look in Visual Studio's Output window for detailed error messages
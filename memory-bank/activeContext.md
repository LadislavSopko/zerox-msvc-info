# Active Context

## Current Development Focus

We're developing a Visual Studio extension that implements a Model Context Protocol (MCP) server to bridge Visual Studio's project system with AI assistants. The extension uses Visual Studio's Service Broker (JSON-RPC 2.0) for communication.

### Important Technical Constraints
- Target framework: .NET Framework 4.8
- Using modern VS extensibility SDK (Microsoft.VisualStudio.Extensibility.Sdk)
- In-process extension (VssdkCompatibleExtension=true)

### Current Status

1. **Service Broker Implementation - COMPLETED**
   - Implemented clean MCP service architecture following VS extensibility patterns
   - Created IMCPService interface with static configuration
   - Simplified MCPService implementation with BrokeredServiceConfiguration
   - Service registration using ProfferBrokeredService
   - Test commands updated to use ServiceBroker.GetProxyAsync

2. **Core Functionality - COMPLETED**
   - Bidirectional path translation (Windows/WSL)
   - Solution/project introspection
   - Symbol finding and navigation
   - Document outline extraction
   - Resource listing and reading
   - Full Roslyn integration for code analysis

3. **HTTP Transport - COMPLETED**
   - Implemented HttpListener-based server
   - JSON-RPC request routing to MCPService
   - Start/Stop/Status commands for HTTP server
   - Test command for validation
   - Successfully tested with curl
   - Default port 3000 configuration
   - No authentication (following MCP standard practice)

### Architecture Insights

- Visual Studio Service Broker already uses JSON-RPC 2.0
- Our MCPService is fully JSON-RPC compliant
- No complex bridging needed - just HTTP transport layer
- Claude Desktop uses stdio transport without authentication
- HTTP transport is optional for external access

### Next Steps

1. **Configuration & Refinements**
   - Add dynamic port/host configuration options
   - Improve HttpServer to MCPService integration
   - Add graceful shutdown handling

2. **Documentation**
   - Create HTTP endpoint usage examples
   - Document available MCP methods and responses
   - Add setup instructions for external clients

3. **Testing & Validation**
   - Test with Claude Desktop or other MCP clients
   - Performance testing with large solutions
   - Error handling improvements

### Available MCP Endpoints

The HTTP server exposes the following MCP methods at `http://localhost:3000/jsonrpc`:

- `initialize` - Initialize MCP session
- `tools/list` - List available tools
- `tools/call` - Call a specific tool
- `resources/list` - List available resources
- `resources/read` - Read a resource

Available tools:
- `get_solution_projects` - Get all projects in the solution
- `find_symbols` - Find symbols by name
- `get_symbol_at_location` - Get symbol at specific location
- `get_document_outline` - Get document symbols/outline
- `translate_path` - Translate paths between Windows/WSL formats
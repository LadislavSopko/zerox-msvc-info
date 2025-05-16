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
   - Removed unnecessary abstractions (factory, separate RPC target)
   - Service registration using ProfferBrokeredService
   - Test commands updated to use ServiceBroker.GetProxyAsync

2. **Core Functionality - COMPLETED**
   - Bidirectional path translation (Windows/WSL)
   - Solution/project introspection
   - Symbol finding and navigation
   - Document outline extraction
   - Resource listing and reading

3. **Next Phase: HTTP Transport**
   - Add HTTP endpoint for external access
   - Use HttpListener for simple implementation
   - Forward JSON-RPC requests to existing MCPService
   - No authentication required initially (based on MCP standard practice)

### Architecture Insights

- Visual Studio Service Broker already uses JSON-RPC 2.0
- Our MCPService is fully JSON-RPC compliant
- No complex bridging needed - just HTTP transport layer
- Claude Desktop uses stdio transport without authentication
- HTTP transport is optional for external access

### Next Steps

1. **Implement HTTP Transport**
   - Create HttpListener-based server
   - Route JSON-RPC requests to MCPService
   - Configure port and startup settings
   - Test with HTTP clients

2. **Documentation**
   - Update setup instructions
   - Document HTTP endpoint usage
   - Create integration examples
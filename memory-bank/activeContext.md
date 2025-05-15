# Active Context

## Current Development Focus

We're developing a Visual Studio extension that implements a Model Context Protocol (MCP) server to bridge Visual Studio's project system with AI assistants. The extension uses Visual Studio's Service Broker (JSON-RPC 2.0) for communication.

### Important Technical Constraints
- Target framework: .NET Framework 4.8
- Using modern VS extensibility SDK (Microsoft.VisualStudio.Extensibility.Sdk)
- In-process extension (VssdkCompatibleExtension=true)

### Current Status

1. **Project Structure Reorganization**
   - Created separate Msvc.Info.Core project for shared functionality
   - Added Msvc.Info.Tests for unit tests
   - Moved path translation services to Core project

2. **Path Translation Implementation**
   - Implemented bidirectional translation between Windows and WSL paths
   - Added support for URI path formats
   - Created unit tests for path translation functionality
   - Added relative path calculation functionality

3. **Service Broker Integration**
   - Updated approach to use Visual Studio's Service Broker instead of direct TCP
   - Benefits include better authentication, discovery, and lifecycle management

### Next Steps

1. **Complete MCP Service Implementation**
   - Implement core MCP methods for solution introspection
   - Register services with VS Service Broker
   - Add project/solution structure querying functionality

2. **Solution Structure API**
   - Develop APIs to extract and represent solution/project structure
   - Implement project relationship mapping
   - Create file-to-project mapping functionality

3. **Testing and Documentation**
   - Expand test coverage
   - Document API interfaces and usage patterns
   - Add integration tests
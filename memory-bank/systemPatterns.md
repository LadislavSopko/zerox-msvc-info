# System Patterns

## Architecture Overview

The MSVC MCP Server follows a modular architecture with clear separation of concerns:

```
Extension Entry Point → Service Registration → MCP Services → Core Services
                     ↓                       ↗              ↘
              HTTP Transport               Service Broker    Roslyn APIs
```

### Component Structure

- **Extension Layer**: VS extension entry point, lifecycle management, VS service integration
- **MCP Service Layer**: Protocol implementation, request handling, response formatting
- **HTTP Transport Layer**: HttpListener-based server for external access
- **Service Broker Layer**: VS native JSON-RPC communication
- **Core Services Layer**: Reusable functionality independent of VS and MCP
- **Test Layer**: Unit and integration tests for all components

## Design Patterns

### Dependency Injection

The system uses dependency injection throughout to enable:
- Loose coupling between components
- Easier testing with mock implementations
- Configuration flexibility

Example:
```csharp
// Service registration
serviceCollection.AddScoped<IPathTranslationService, PathTranslationService>();
serviceCollection.AddScoped<IExtensionLogger, ExtensionLogger>();

// Constructor injection
public class McpService
{
    private readonly IPathTranslationService _pathTranslationService;
    
    public McpService(IPathTranslationService pathTranslationService)
    {
        _pathTranslationService = pathTranslationService;
    }
}
```

### Interface Segregation

Services are defined by focused interfaces representing specific responsibilities:
- `IPathTranslationService`: Path conversion between environments
- `IExtensionLogger`: Extension-specific logging capabilities

### Service Pattern

Core functionality is encapsulated in service classes with clear responsibilities:
- `PathTranslationService`: Handles all path format conversions
- `MCPService`: Implements MCP protocol handling
- `MCPHttpServer`: Provides HTTP transport for external access

## Communication Patterns

### Dual Transport Architecture

The extension supports two communication channels:

1. **VS Service Broker** (Primary):
   - JSON-RPC 2.0 message format
   - Service registration for discovery
   - Native VS authentication and lifecycle management
   - Used by VS internal tools and extensions

2. **HTTP Transport** (Secondary):
   - HttpListener on configurable port (default 3000)
   - Same JSON-RPC 2.0 protocol
   - No authentication (following MCP standard)
   - Used by external tools like Claude Desktop

### Error Handling

Robust error handling strategy:
- Detailed error logging with context
- Graceful failure with meaningful error messages
- Exception propagation only when necessary

## Testing Strategy

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test component interactions
- **Mock Objects**: Used for dependency isolation
- **Theory Tests**: Data-driven tests for algorithmic components
# System Patterns

## Architecture Overview

The MSVC MCP Server follows a modular architecture with clear separation of concerns:

```
Extension Entry Point ’ Service Registration ’ MCP Services ’ Core Services
```

### Component Structure

- **Extension Layer**: VS extension entry point, lifecycle management, VS service integration
- **MCP Server Layer**: Protocol implementation, request handling, response formatting
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
- `McpService`: Implements MCP protocol handling

## Communication Patterns

### Service Broker Integration

The extension communicates with clients using VS Service Broker:
- JSON-RPC 2.0 message format
- Service registration for discovery
- Proper authentication and lifecycle management

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
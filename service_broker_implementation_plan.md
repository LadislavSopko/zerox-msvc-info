# AI-Focused MCP Server Implementation Plan

## Project Mission
Create a minimal viable proof of concept (POC) of an MCP server that bridges Visual Studio's project system with AI assistants, enabling context-aware AI tools to work effectively with VS solutions.

## Current Issues

1. **Complexity**: Manual pipe handling and JSON-RPC setup adds unnecessary complexity
2. **Interface Mismatches**: Service factory implementation doesn't match expected interfaces
3. **Multiple Approaches**: Mix of different Service Broker registration techniques
4. **Maintainability**: Complex code is harder to maintain and debug

## Implementation Goal

Create a simplified approach to expose our JSON-RPC 2.0 service directly through Visual Studio's Service Broker, focusing on providing the essential solution context for AI agents.

## Why This Approach

1. **AI-Specific Focus**: Prioritize features specifically needed by AI tools to understand VS solutions
2. **Simplicity First**: Implement the simplest possible working version to demonstrate the concept
3. **Leverage VS Infrastructure**: Use built-in Service Broker capabilities instead of custom communication

Visual Studio's Service Broker provides:
- Automatic JSON-RPC communication handling
- Service discovery and lifecycle management
- Cross-process communication where needed
- Standard interface for service registration

## Essential Features (MVP)

1. **Solution Structure**
   - Project hierarchy and relationships
   - Basic metadata (project types, languages, file lists)
   - Enables AI to understand overall codebase organization

2. **Path Translation**
   - Windows ↔ WSL bidirectional translation
   - Critical for AI to reference files correctly when switching contexts

3. **Service Broker Connectivity**
   - Simple, reliable implementation
   - Discoverable by AI tools

## Implementation Steps

### Step 1: Simplify the Service Descriptor

Replace complex ServiceRpcDescriptor setup with the simpler ServiceJsonRpcDescriptor.

**File**: `mcp_service_descriptor.cs`

```csharp
internal static class MCPServiceDescriptor
{
    public static readonly ServiceMoniker Moniker = new ServiceMoniker("Microsoft.VisualStudio.MCP", new Version(1, 0));
    
    public static readonly ServiceJsonRpcDescriptor Descriptor = new ServiceJsonRpcDescriptor(
        Moniker,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceAudience.AllClientsIncludingGuests);
}
```

### Step 2: Update the Service Factory

Simplify to match the standard IServiceFactory interface.

**File**: `mcp_service_descriptor.cs`

```csharp
internal class MCPServiceFactory : IServiceFactory
{
    private readonly IMCPService _mcpService;

    public MCPServiceFactory(IMCPService mcpService)
    {
        _mcpService = mcpService;
    }

    public Task<object?> CreateAsync(ServiceActivationOptions options, CancellationToken cancellationToken)
    {
        return Task.FromResult<object?>(_mcpService);
    }
}
```

### Step 3: Simplify the Service Registration

Use the direct service proffering approach for cleaner registration.

**File**: `ExtensionEntrypoint.cs`

```csharp
protected override async Task InitializeAsync(CancellationToken cancellationToken)
{
    // Get the service broker container
    var serviceBrokerContainer = await AsyncServiceProvider.GlobalProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
    
    // Register the service directly with the service broker
    serviceBrokerContainer.Proffer(
        MCPServiceDescriptor.Descriptor,
        (moniker, options, broker, cancellationToken) => 
            new ValueTask<object?>(_mcpService));
}
```

### Step 4: Implement Testing Commands

Create commands to test the MCP service through the Service Broker.

**File**: `mcp_service_commands.cs`

```csharp
// Connect to MCP service via Service Broker's proxy
var connectionResult = await serviceBroker.GetProxyAsync<IMCPService>(
    MCPServiceDescriptor.Descriptor, 
    cancellationToken: cancellationToken);

using var serviceConnection = connectionResult;
var mcpService = serviceConnection.Proxy;

// Test initialize
var initResult = await mcpService.InitializeAsync(new { }, cancellationToken);
```

### Step 5: Ensure Core MCP Protocol Methods

Verify our implementation supports these essential MCP methods:

1. **Initialize**: Advertise capabilities and protocol version
2. **tools/list**: List available solution/code analysis tools
3. **tools/call**: Execute tools (especially get_solution_projects)
4. **resources/list**: List available solution/project resources
5. **resources/read**: Read resource content (project info, file content)

### Step 6: Add Path Translation Integration

Ensure the Path Translation service is:
1. Properly injected into the MCP service
2. Used in relevant path-handling operations
3. Accessible via appropriate tools/methods

## Testing Strategy

1. **Command Testing**: Use the command UI to verify basic functionality
2. **Real Project Test**: Test against a non-trivial VS solution
3. **Path Translation Test**: Verify Windows ↔ WSL conversion accuracy
4. **AI Tool Simulation**: Test a typical AI workflow:
   - Connect to service
   - Get solution structure
   - Translate paths
   - Access file content

## Expected Benefits

1. **AI Context Awareness**: AI tools can understand VS solution structure
2. **Cross-Platform Compatibility**: Seamless path translation between environments
3. **Simplified Integration**: Standard protocol for AI tools to access VS data
4. **Proof of Concept**: Demonstrates the value of VS-AI integration
5. **Foundation for Future**: Establishes a base for more advanced features

## Required NuGet Packages

- Microsoft.ServiceHub.Framework
- Microsoft.VisualStudio.Shell.ServiceBroker
- StreamJsonRpc 
- Microsoft.VisualStudio.LanguageServices (for Roslyn workspace access)
- Microsoft.CodeAnalysis (for code analysis capabilities)
# Technology Context: Visual Studio MCP Extension

## Overview
Creating a Visual Studio extension that implements a Model Context Protocol (MCP) server to expose VS solution/project data and symbol navigation to AI tools.

## Architecture

### Core Components
- **In-Process VS Extension**: Single package that runs inside devenv.exe
- **MCP Server**: JSON-RPC 2.0 server exposing VS functionality  
- **TCP Socket Communication**: Direct TCP connection on localhost:3001
- **Roslyn Integration**: Direct access to VS Workspace and symbol APIs

### Data Flow
```
AI Tool (MCP Client) → TCP:3001 → VS Extension (MCP Server) → Roslyn APIs → VS Solution Data
```

## Key Technologies

### Visual Studio SDK
- **Microsoft.VisualStudio.SDK**: Provides VS integration points
- **AsyncPackage**: Entry point for extension lifecycle
- **In-process execution**: Full access to VS APIs without IPC overhead

### Roslyn APIs
- **VisualStudioWorkspace**: Access to open solution/projects
- **SymbolFinder**: Symbol discovery and navigation
- **SemanticModel**: Code analysis and symbol information

### Model Context Protocol (MCP)
- **JSON-RPC 2.0**: Standard protocol for communication
- **Tools**: Symbol search, location queries
- **Resources**: Solution/project metadata, file content

### Implementation Details
- **No Brokered Services needed**: Direct TCP server eliminates IPC complexity
- **Single process**: All logic runs in-process for performance
- **Native VS integration**: Leverages existing workspace state

## Benefits
- **Simplicity**: One extension, one process, direct API access
- **Performance**: No serialization/deserialization overhead
- **Completeness**: Full access to VS symbol tables and project data
- **Standards-based**: Implements official MCP specification

## Use Cases
- AI code analysis with full VS context
- Symbol navigation for AI coding assistants
- Project structure understanding for AI tools
- Real-time code intelligence for external tools

This approach provides a clean, efficient bridge between Visual Studio's rich development environment and AI tools that need deep code context.

## Cross-Platform Compatibility

- Must handle Windows paths (`C:\path\to\file.cs`)
- Must translate to WSL paths (`/mnt/c/path/to/file.cs`)
- Must account for case sensitivity differences
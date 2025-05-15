# Implementation Notes for MCP Server Cleanup (Phase 1)

This document outlines the specific steps and considerations for implementing Phase 1 of the restart plan - cleaning up the codebase by removing unnecessary complexity.

## Phase 1: Cleanup Implementation Steps

### 1. Move Files to to_delete Folder for Reference

Rather than permanently deleting complex/overengineered files, we'll move them to a `to_delete` folder as references. This ensures we don't lose any valuable patterns or approaches.

**Files to move:**

```
From Zerox.Msvc.Core/Core:
- SymbolGraphBuilder.cs 
- SymbolGraphModels.cs
- SymbolGraphEnums.cs
- RoslynWorkspaceManager.cs
- HybridSolutionAnalyzer.cs
- SolutionDataIntrospectionService.cs

From Zerox.Msvc.Core/Api/Controllers:
- ApiController.cs (will be recreated)
```

**Test files to move to to_delete/Tests:**

```
From Zerox.Msvc.Core.Tests/Core:
- HybridSolutionAnalyzerTests.cs
- SolutionDataIntrospectionServiceTests.cs
- SymbolGraphModelsTests.cs
- RoslynWorkspaceManagerTests.cs
```

### 2. Create Service Interface Structure

We'll create the new simplified service interfaces based on the restart plan. Initially, we'll just create the interface files to establish the structure:

```
Zerox.Msvc.Core/Core/Services/
- ISolutionStructureService.cs
- ISymbolLocationService.cs
- IPathTranslationService.cs
- IProjectCommandService.cs
```

### 3. Update the Core Project File

The Zerox.Msvc.Core.csproj file will need to be updated to reference the new services folder and remove references to files that have been moved to the to_delete folder.

### 4. Initial Implementation of ISolutionStructureService

As a first step toward rebuilding, we'll implement the ISolutionStructureService interface and a minimal version of the service to access basic solution structure information.

## Implementation Approach for ISolutionStructureService

1. Create a clean, focused interface with only the essential methods:
   - `GetSolutionInfoAsync()`
   - `GetProjectsAsync()`
   - `GetProjectFilesAsync()`

2. Implement a minimal, reliable service that:
   - Uses direct Project Query API calls
   - Follows "one level down" principle strictly
   - Implements timeouts and proper cancellation
   - Handles errors gracefully

3. Create simple data models:
   - `SolutionInfo`
   - `ProjectInfo`
   - `FileInfo`

4. Defer file content operations to AI assistant direct file access

## Phase 1 Success Criteria

The Phase 1 cleanup will be considered successful when:

1. Overly complex files are moved to the to_delete folder
2. New service interfaces are created
3. ISolutionStructureService is implemented with basic functionality
4. The project builds successfully with the new structure
5. Basic tests can run against ISolutionStructureService

## Next Steps After Phase 1

1. Implement remaining service interfaces
2. Update JSON-RPC and API controllers
3. Connect the simplified MCP server to the new services
4. Update testing framework to match new architecture

## Detailed Implementation Plan

### Step 1: Copy Legacy Files to to_delete Folder

First, we'll copy the files we want to preserve for reference to the to_delete folder:

```bash
# Core files
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core/Core/SymbolGraphBuilder.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Core/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core/Core/SymbolGraphModels.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Core/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core/Core/SymbolGraphEnums.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Core/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core/Core/RoslynWorkspaceManager.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Core/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core/Core/HybridSolutionAnalyzer.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Core/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core/Core/SolutionDataIntrospectionService.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Core/

# Controller files
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core/Api/Controllers/ApiController.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Api/Controllers/

# Test files
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core.Tests/Core/HybridSolutionAnalyzerTests.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Tests/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core.Tests/Core/SolutionDataIntrospectionServiceTests.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Tests/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core.Tests/Core/SymbolGraphModelsTests.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Tests/
cp /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/Zerox.Msvc.Core.Tests/Core/RoslynWorkspaceManagerTests.cs /mnt/c/Projekty/AI_Works/Msvc.Mcp/Zerox.Msvc.Mcp/to_delete/Tests/
```

### Step 2: Create Service Interfaces

Next, we'll create the service interfaces:

#### ISolutionStructureService.cs

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zerox.Msvc.Core.Services
{
    public interface ISolutionStructureService
    {
        Task<SolutionInfo> GetSolutionInfoAsync(CancellationToken cancellationToken);
        Task<List<ProjectInfo>> GetProjectsAsync(CancellationToken cancellationToken);
        Task<List<FileInfo>> GetProjectFilesAsync(string projectId, CancellationToken cancellationToken);
    }
}
```

#### ISymbolLocationService.cs

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zerox.Msvc.Core.Services
{
    public interface ISymbolLocationService
    {
        Task<List<SymbolLocation>> FindSymbolsAsync(string symbolName, string symbolKind = null, CancellationToken cancellationToken = default);
        Task<SymbolInfo> GetSymbolAtLocationAsync(string filePath, int line, int column, CancellationToken cancellationToken = default);
        Task<bool> InitializeWorkspaceAsync(string solutionPath, CancellationToken cancellationToken = default);
    }
}
```

#### IPathTranslationService.cs

```csharp
namespace Zerox.Msvc.Core.Services
{
    public enum PathFormat
    {
        Auto,
        Windows,
        Wsl,
        Uri
    }

    public interface IPathTranslationService
    {
        string TranslatePath(string path, PathFormat sourceFormat, PathFormat targetFormat);
        string GetRelativePath(string basePath, string fullPath, PathFormat targetFormat = PathFormat.Windows);
    }
}
```

#### IProjectCommandService.cs

```csharp
using System.Threading;
using System.Threading.Tasks;

namespace Zerox.Msvc.Core.Services
{
    public interface IProjectCommandService
    {
        Task<CommandResult> BuildProjectAsync(string projectId, CancellationToken cancellationToken);
        Task<CommandResult> RunTestsAsync(string projectId, CancellationToken cancellationToken);
    }
}
```

### Step 3: Create Basic Model Classes

We'll create simplified model classes to replace the complex ones:

```csharp
// Add to Model.cs or create a new file

public class SolutionInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public List<ProjectInfo> Projects { get; set; } = new List<ProjectInfo>();
}

public class ProjectInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string Type { get; set; }
}

public class FileInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Language { get; set; }
}

public class SymbolLocation
{
    public string FilePath { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
}

public class SymbolInfo
{
    public string Name { get; set; }
    public string Kind { get; set; }
    public SymbolLocation Location { get; set; }
    public string ContainingType { get; set; }
}

public class CommandResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Output { get; set; }
}
```

### Step 4: Implementation of SolutionStructureService

This will be our focus in Phase 1:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Zerox.Msvc.Core.Services
{
    public class SolutionStructureService : ISolutionStructureService
    {
        private readonly IProjectQueryableSpaceAccessor _accessor;
        private readonly ILogger<SolutionStructureService> _logger;
        private readonly QueryableSpace _workspace;

        public SolutionStructureService(
            IProjectQueryableSpaceAccessor accessor,
            ILogger<SolutionStructureService> logger)
        {
            _accessor = accessor;
            _logger = logger;
            _workspace = accessor?.WorkSpace;
        }

        public async Task<SolutionInfo> GetSolutionInfoAsync(CancellationToken cancellationToken)
        {
            if (_workspace == null)
                return null;

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var combinedToken = combinedCts.Token;
            
            try
            {
                var solutionResults = await _workspace.Solutions
                    .With(s => new { s.Path, s.Guid, s.DisplayName })
                    .ExecuteQueryAsync(combinedToken);
                    
                var solution = solutionResults.FirstOrDefault();
                if (solution == null)
                    return null;
                    
                return new SolutionInfo
                {
                    Name = solution.DisplayName ?? Path.GetFileNameWithoutExtension(solution.Path),
                    Path = solution.Path
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout getting solution info");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting solution info");
                return null;
            }
        }

        public async Task<List<ProjectInfo>> GetProjectsAsync(CancellationToken cancellationToken)
        {
            if (_workspace == null)
                return new List<ProjectInfo>();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var combinedToken = combinedCts.Token;
            
            try
            {
                var solutionInfo = await GetSolutionInfoAsync(combinedToken);
                if (solutionInfo == null)
                    return new List<ProjectInfo>();
                    
                var projectResults = await _workspace.Projects
                    .With(p => new { p.Id, p.Name, p.Kind, p.Path, p.FullPath })
                    .ExecuteQueryAsync(combinedToken);
                    
                var projects = projectResults
                    .Where(p => !string.IsNullOrEmpty(p.Name) && !string.IsNullOrEmpty(p.FullPath))
                    .Select(p => new ProjectInfo
                    {
                        Id = CreateProjectId(p.Name, p.FullPath, solutionInfo.Path),
                        Name = p.Name,
                        Path = p.FullPath,
                        Type = p.Kind?.ToString() ?? "Unknown"
                    })
                    .ToList();
                    
                return projects;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Timeout getting projects list");
                return new List<ProjectInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects list");
                return new List<ProjectInfo>();
            }
        }

        public async Task<List<FileInfo>> GetProjectFilesAsync(string projectId, CancellationToken cancellationToken)
        {
            if (_workspace == null || string.IsNullOrEmpty(projectId))
                return new List<FileInfo>();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var combinedToken = combinedCts.Token;
            
            try
            {
                var projects = await GetProjectsAsync(combinedToken);
                var targetProject = projects.FirstOrDefault(p => p.Id == projectId);
                if (targetProject == null)
                    return new List<FileInfo>();
                    
                var vsProjects = await _workspace.Projects
                    .With(p => new { p.Id, p.Name, p.FullPath })
                    .ExecuteQueryAsync(combinedToken);
                    
                var vsProject = vsProjects.FirstOrDefault(p => 
                    p.Name == targetProject.Name && 
                    p.FullPath == targetProject.Path);
                    
                if (vsProject?.Id == null)
                    return new List<FileInfo>();
                    
                var files = await _workspace.Projects
                    .Where(p => p.Id == vsProject.Id)
                    .Get(p => p.SourceFiles)
                    .With(f => new { f.Name, f.FilePath })
                    .ExecuteQueryAsync(combinedToken);
                    
                return files
                    .Where(f => !string.IsNullOrEmpty(f.FilePath))
                    .Select(f => new FileInfo
                    {
                        Name = f.Name ?? Path.GetFileName(f.FilePath),
                        Path = f.FilePath,
                        Language = GetLanguageFromPath(f.FilePath)
                    })
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Timeout getting files for project {projectId}");
                return new List<FileInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting files for project {projectId}");
                return new List<FileInfo>();
            }
        }

        // Helper to create stable project IDs
        private string CreateProjectId(string projectName, string projectPath, string solutionPath)
        {
            var solutionDir = Path.GetDirectoryName(solutionPath);
            var relativePath = projectPath;
            
            if (!string.IsNullOrEmpty(solutionDir) && projectPath.StartsWith(solutionDir))
            {
                relativePath = projectPath.Substring(solutionDir.Length).TrimStart('\\', '/');
            }
            
            return $"{projectName}_{relativePath.Replace('\\', '_').Replace('/', '_')}";
        }

        // Simple helper for file language detection
        private string GetLanguageFromPath(string path)
        {
            var extension = Path.GetExtension(path)?.ToLowerInvariant();
            
            switch (extension)
            {
                case ".cs": return "csharp";
                case ".vb": return "vb";
                case ".fs": return "fsharp";
                case ".ts": 
                case ".tsx": return "typescript";
                case ".js":
                case ".jsx": return "javascript";
                case ".html": return "html";
                case ".css": return "css";
                case ".json": return "json";
                case ".xml": return "xml";
                case ".xaml": return "xaml";
                default: return "text";
            }
        }
    }
}
```

### Step 5: Update Project File

We need to update Zerox.Msvc.Core.csproj to include the new Services directory and remove references to files that have been moved.

## Execution Plan for Phase 1

1. Create the to_delete folder structure ✓
2. Copy files to the to_delete folder (for reference) 
3. Create the Services directory ✓
4. Implement service interfaces
5. Create basic model classes
6. Implement SolutionStructureService
7. Update project files to reference new services
8. Build and verify changes

This approach keeps the valuable code patterns for reference while creating a cleaner, more focused implementation that follows the principles outlined in the restart plan.
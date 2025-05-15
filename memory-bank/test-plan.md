# Unit Testing Plan for Roslyn Integration

## Components to Test

- [x] RoslynWorkspaceManager
- [x] SymbolGraphBuilder
- [x] HybridSolutionAnalyzer
- [x] SolutionDataIntrospectionService (Roslyn integration)
- [x] API and JSON-RPC Controllers (Roslyn endpoints)

## Test Checklist

### RoslynWorkspaceManager Tests

- [ ] Test LoadSolutionAsync with valid path returns and caches solution
- [ ] Test LoadSolutionAsync with invalid path returns null
- [ ] Test LoadSolutionAsync handles workspace exceptions gracefully
- [ ] Test GetCompilationAsync returns and caches compilation
- [ ] Test GetCompilationAsync handles exceptions gracefully
- [ ] Test GetSemanticModelAsync returns and caches semantic model
- [ ] Test concurrent access properly uses SemaphoreSlim lock
- [ ] Test ClearCaches removes all cached items
- [ ] Test GetLastModifiedTime returns correct timestamp

### SymbolGraphBuilder Tests

- [ ] Test BuildSymbolGraphAsync with basic project returns graph with nodes
- [ ] Test inheritance relationships create "Inherits" edges
- [ ] Test interface implementation creates "Implements" edges
- [ ] Test method return types create "Returns" edges
- [ ] Test parameter types create "Uses" edges with labels
- [ ] Test symbol references are found and added to graph
- [ ] Test ToRoslynFilter correctly converts all SymbolFilterWithDependencies values
- [ ] Test different SymbolFilter values include/exclude appropriate symbols
- [ ] Test cancellation stops processing without exception
- [ ] Test GetSymbolId generates consistent IDs
- [ ] Test GetSymbolKindString correctly identifies all symbol types

### HybridSolutionAnalyzer Tests

- [ ] Test GetSolutionDataAsync combines data from both APIs when available
- [ ] Test fallback to Roslyn when Project Query API fails
- [ ] Test fallback to Project Query API when Roslyn fails
- [ ] Test minimal data returned when both APIs fail
- [ ] Test different DataLoadLevel values return appropriate data amounts
- [ ] Test PopulateProjectSymbolInfoAsync adds symbol data to project
- [ ] Test thread safety with concurrent operations

### SolutionDataIntrospectionService Tests

- [ ] Test GetProjectSymbolGraphAsync returns symbol graph for valid project
- [ ] Test GetProjectSymbolGraphAsync returns null when Roslyn not available
- [ ] Test GetProjectSymbolGraphAsync handles exceptions gracefully
- [ ] Test constructor with only Roslyn components (no Project Query API)
- [ ] Test PopulateSolutionInfoAsync falls back to Roslyn when _workspace is null
- [ ] Test PopulateProjectsBasicInfoAsync falls back to Roslyn when _workspace is null
- [ ] Test PopulateProjectFilesAsync falls back to Roslyn when _workspace is null
- [ ] Test all protected methods handle null _workspace

### Controller Integration Tests

- [ ] Test GetProjectSymbolGraph returns valid response for valid project
- [ ] Test GetProjectSymbolGraph returns NotFound for invalid project
- [ ] Test GetProjectSymbolGraph handles service exceptions gracefully
- [ ] Test JSON-RPC symbol graph endpoint returns correct data
- [ ] Test JSON-RPC symbol graph endpoint handles errors appropriately

## Integration Tests

- [ ] End-to-end test with small test solution
- [ ] Performance test with larger solution
- [ ] Fallback test when Project Query API unavailable
- [ ] Memory usage test with symbol graph generation
- [ ] Concurrent request handling test

## Mocking Requirements

- [ ] Create mockable interfaces for external dependencies
- [ ] Mock MSBuildWorkspace for unit testing
- [ ] Mock Solution, Project, Document objects
- [ ] Create wrappers for static SymbolFinder methods
- [ ] Create synthetic C# code for symbol relationship testing

## Test Data Needs

- [ ] Simple C# code with inheritance, interfaces, methods
- [ ] Small test solution with multiple projects
- [ ] Sample solution with known symbol relationships
- [ ] Large solution for performance testing
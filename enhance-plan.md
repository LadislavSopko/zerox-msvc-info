# MCP Tools Enhancement Plan

## Priority Focus Areas

1. **Navigation** - Solution/project/file hierarchy browsing
2. **Symbol Search** - Find definitions, usages, relationships
3. **Editor Context** - Active file, selections, modifications
4. **Project Commands** - Build, clean, test operations
5. **Package Management** - NuGet package operations

## Current State Analysis

### What we have
- [x] 5 working tools (get_solution_projects, find_symbols, get_symbol_at_location, get_document_outline, translate_path)
- [x] Basic path translation functionality
- [x] HTTP transport working on port 3000
- [x] Service Broker integration
- [x] JSON-RPC 2.0 compliance

### What's missing
- [ ] Path format handling is inconsistent
- [ ] Limited navigation capabilities
- [ ] No build/compilation commands
- [ ] No editor context access
- [ ] Missing proper MCP error responses

## Path Handling Fix

### Problem
- Mixed path formats without client control
- No format specification in requests

### Solution
- Add `pathFormat` parameter to file-related tools
- Support formats: "Windows", "WSL", "URI"
- Convert responses to requested format

### Tools to Update
- `get_document_outline`
- `get_symbol_at_location`
- `find_symbols`

## Tools to Implement

### Priority 1: Navigation
- [ ] `get_solution_tree()` - Solution overview with projects
- [ ] `get_solution_tree(projectName)` - Project files and details
- [ ] `get_project_references` - Project dependencies

### Priority 2: Symbol Search
- [ ] `find_symbol_definition` - Where symbol is defined
- [ ] `find_symbol_usages` - Where symbol is used
- [ ] `get_method_calls` - Methods this method calls
- [ ] `get_method_callers` - Methods calling this method
- [ ] `get_inheritance` - Direct inheritance relationships

### Priority 3: Editor Context
- [ ] `get_active_file` - Currently open file
- [ ] `get_selection` - Current text selection
- [ ] `get_modified_files` - Files with unsaved changes
- [ ] `check_selection` (shortcut `/check`) - Combined file + selection

### Priority 4: Project Commands
- [ ] `solution_build` - Build entire solution
- [ ] `solution_clean` - Clean solution
- [ ] `solution_test` - Run all tests
- [ ] `project_build` - Build specific project
- [ ] `project_clean` - Clean specific project
- [ ] `project_test` - Run project tests

### Priority 5: Package Management
- [ ] `package_add` - Add NuGet package
- [ ] `package_remove` - Remove package
- [ ] `package_update` - Update package version
- [ ] `package_list` - List installed packages
- [ ] `package_restore` - Restore packages

## Implementation Phases

### Phase 1: Navigation & Search
- Implement solution tree navigation
- Add symbol search tools
- Fix path format handling

### Phase 2: Editor Integration
- Add editor context tools
- Create shortcut commands

### Phase 3: Build & Package Commands
- Add build/test operations
- Implement package management

### Phase 4: Polish
- Fix MCP compliance (empty arrays for unsupported features)
- Add comprehensive error handling
- Optimize for large solutions

## Key Design Principles

- Simple, focused tools (one job each)
- No file content operations (you handle directly)
- Fast response times
- Clear error messages
- MCP specification compliance

## Success Criteria

- [ ] Efficient navigation in large solutions
- [ ] Quick symbol search and relationships
- [ ] Access to editor state
- [ ] Build/test automation
- [ ] Package management
- [ ] Proper path handling
- [ ] MCP compliance

## Next Steps

1. Review and approve this plan
2. Start with Phase 1 (Navigation & Search)
3. Test with real VS solutions
4. Iterate based on usage
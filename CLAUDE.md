# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Mission

The MSVC MCP Server bridges Visual Studio's project system with AI assistants, focusing on:

1. **Structure Navigation**: Provide solution/project structures and relationships, not content
2. **Path Translation**: Bidirectional translation between Windows and WSL paths
3. **Operation Execution**: Enable AIs to trigger builds, tests, and commands they can't perform directly
4. **Simplicity First**: Lightweight, stateless APIs over complex symbol analysis
5. **Standard Compliance**: Follow MCP specification for maximum compatibility

## Project Overview

This repository contains a Visual Studio extension (MSVC MCP Server) that implements the Model Context Protocol (MCP) server within Visual Studio. It creates a bridge between Visual Studio's project system and AI assistants, enabling context-aware AI tools to work with VS solutions.

## Build and Development Commands

You can not RUN and TEST project IT is for windonws!!!

## Architecture Overview


## Path Translation

When working with path translation between Windows and WSL:

1. Windows paths (`C:\Path\To\File.cs`) need to be converted to WSL paths (`/mnt/c/Path/To/File.cs`)
2. Path separators (`\` vs `/`) need to be handled correctly
3. Consider case sensitivity differences between Windows and Linux


## Implementation Note

The project is implementing the official Model Context Protocol (MCP) specification. When making changes:

1. Always refer to the MCP specification: https://modelcontextprotocol.io/llms-full.txt
2. Ensure compliance with JSON-RPC 2.0 message format
3. Consider both Windows and WSL environments in path handling

# CODING & INTERACTION NOTES

## Collaboration Rules

When working with Claude Code on this project, follow these operational modes and context rules:

### Operational Modes

1. **PLAN Mode**
   - Default starting mode for all interactions
   - Used for discussing implementation details without making code changes
   - Claude will print `# Mode: PLAN` at the beginning of each response
   - Outputs relevant portions of the plan based on current context level
   - If action is requested, Claude will remind you to approve the plan first

2. **ACT Mode**
   - Only activated when the user explicitly types `ACT`
   - Used for making actual code changes based on the approved plan
   - Claude will print `# Mode: ACT` at the beginning of each response
   - Automatically returns to PLAN mode after each response
   - Can be manually returned to PLAN mode by typing `PLAN`

3. **Documentation Updates**
   - Memory-bank files should only be updated when explicitly requested
   - Request updates with command `SNAPSHOT`
   - Updates should reflect the current development state and decisions
   - After updating memory-bank files, return to PLAN mode

### Context Optimization Rules

To optimize token usage and maintain focus, the following context levels are available:

1. **Full Context** (Command: `FULL_CONTEXT`)
   - Includes comprehensive memory bank details and complete project plan
   - Use only at the beginning of a session or when major directional changes occur
   - Provides holistic project understanding but consumes significant tokens

2. **Module Context** (Command: `MODULE_CONTEXT [module_name]`)
   - Focuses on a specific module (e.g., Scanner, Storage, Analyzer)
   - Includes module-specific interfaces, requirements, and implementation details
   - Summarizes other modules' relationships to the current module
   - Ideal when working deeply on one component

3. **Task Context** (Command: `TASK_CONTEXT`)
   - Narrows focus to just the immediate implementation task
   - Includes only the specific code, interfaces, and requirements for the current task
   - Most token-efficient option for focused coding work
   - Default for most implementation tasks

When switching to a new task or module, Claude will ask which context level to use if not specified.

This workflow ensures:
1. Deliberate development with clear approval steps before code changes
2. Appropriate context level to balance comprehensive understanding with token efficiency
3. Consistent documentation through memory bank updates
4. Clear separation between planning and implementation phases

## Memory Bank

The `memory-bank` directory contains crucial documentation and progress tracking for the project. This documentation system is designed to maintain perfect continuity between development sessions.

### Memory Bank Structure

The Memory Bank consists of these key files (all in Markdown format):

1. **productContext.md**: Defines why this project exists, problems it solves, and user experience goals
2. **activeContext.md**: Captures the current work focus, recent changes, and active decisions
3. **systemPatterns.md**: Documents system architecture, key technical decisions, and design patterns
4. **techContext.md**: Details technologies used, development setup, and technical constraints
5. **progress.md**: Tracks what works, what's left to build, known issues, and project evolution
6. **docs/**: Contains detailed architectural and implementation plans

Additional context files may be created when they help organize complex feature documentation, integration specifications, API documentation, etc.

### Memory Bank Rules

1. **Documentation Importance**
   - The Memory Bank is the single source of truth for project state
   - Every Claude instance MUST read ALL memory bank files at the start of each task
   - Documentation must be maintained with precision and clarity

2. **Update Triggers**
   - Updates occur when discovering new project patterns
   - After implementing significant changes
   - When requested with the command `SNAPSHOT`
   - When context needs clarification

3. **Update Process**
   - Review ALL Memory Bank files, even if some don't require updates
   - Focus particularly on activeContext.md and progress.md
   - Document the current state, next steps, insights, and patterns
   - Return to PLAN mode after updates

This documentation system ensures that development can continue seamlessly across different sessions, as it provides complete context about the project's current state and future direction.
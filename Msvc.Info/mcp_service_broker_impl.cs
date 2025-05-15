using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Runtime.Serialization;
using StreamJsonRpc;
using System.IO;

namespace Msvc.Info
{
    /// <summary>
    /// MCP Service interface definition for JSON-RPC
    /// </summary>
    public interface IMCPService
    {
        /// <summary>
        /// Initialize the MCP service
        /// </summary>
        Task<object> InitializeAsync(object parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// List available tools
        /// </summary>
        Task<object> ListToolsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Call a specific tool
        /// </summary>
        Task<object> CallToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default);

        /// <summary>
        /// List available resources
        /// </summary>
        Task<object> ListResourcesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Read a specific resource
        /// </summary>
        Task<object> ReadResourceAsync(string uri, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of MCP Service using Visual Studio Service Broker
    /// </summary>
    internal class MCPServiceBrokerImpl : IMCPService, IDisposable
    {
        private readonly VisualStudioWorkspace? _workspace;
        private readonly ILogger<MCPServiceBrokerImpl> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public MCPServiceBrokerImpl(ILogger<MCPServiceBrokerImpl> logger)
        {
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Get Visual Studio workspace through Service Provider
            _workspace = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var componentModel = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                return componentModel?.GetService<VisualStudioWorkspace>();
            });

            _logger.LogInformation("MCP Service Broker implementation initialized");
        }

        public Task<object> InitializeAsync(object parameters, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("MCP Service initialized by client");
            
            return  Task.FromResult( new
            {
                protocolVersion = "2025-03-26",
                capabilities = new
                {
                    tools = new
                    {
                        listTools = true,
                        callTool = true
                    },
                    resources = new
                    {
                        listResources = true,
                        readResource = true
                    }
                },
                serverInfo = new
                {
                    name = "VisualStudio MCP Service Broker",
                    version = "1.0.0"
                }
            } as object);
        }

        public Task<object> ListToolsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing available tools");
            
            ToolDefinition[] tools = new[]
            {
                new ToolDefinition
                {
                    name = "find_symbols",
                    description = "Find symbols in the current solution",
                    inputSchema = new InputSchema
                    {
                        type = "object",
                        properties = new Dictionary<string, SchemaProperty>
                        {
                            ["name"] = new SchemaProperty { type = "string", description = "Symbol name to search for" },
                            ["kind"] = new SchemaProperty { type = "string", description = "Symbol kind (optional)" }
                        },
                        required = new[] { "name" }
                    }
                },
                new ToolDefinition
                {
                    name = "get_symbol_at_location",
                    description = "Get symbol information at a specific location",
                    inputSchema = new InputSchema
                    {
                        type = "object",
                        properties = new Dictionary<string, SchemaProperty>
                        {
                            ["filePath"] = new SchemaProperty { type = "string", description = "File path" },
                            ["line"] = new SchemaProperty { type = "integer", description = "Line number (1-based)" },
                            ["column"] = new SchemaProperty { type = "integer", description = "Column number (1-based)" }
                        },
                        required = new[] { "filePath", "line", "column" }
                    }
                },
                new ToolDefinition
                {
                    name = "get_solution_projects",
                    description = "Get all projects in the current solution",
                    inputSchema = new InputSchema
                    {
                        type = "object",
                        properties = new Dictionary<string, SchemaProperty>()
                    }
                },
                new ToolDefinition
                {
                    name = "get_document_outline",
                    description = "Get outline/symbols of a document",
                    inputSchema = new InputSchema
                    {
                        type = "object",
                        properties = new Dictionary<string, SchemaProperty>
                        {
                            ["filePath"] = new SchemaProperty { type = "string", description = "File path" }
                        },
                        required = new[] { "filePath" }
                    }
                }
            };

            return Task.FromResult( new { tools } as object);
        }

        public async Task<object> CallToolAsync(string toolName, JsonElement arguments, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing tool: {ToolName}", toolName);

            try
            {
                switch (toolName)
                {
                    case "find_symbols":
                        return await FindSymbolsAsync(arguments);
                    
                    case "get_symbol_at_location":
                        return await GetSymbolAtLocationAsync(arguments);
                    
                    case "get_solution_projects":
                        return await GetSolutionProjectsAsync(arguments);
                    
                    case "get_document_outline":
                        return await GetDocumentOutlineAsync(arguments);
                    
                    default:
                        _logger.LogWarning("Unknown tool: {ToolName}", toolName);
                        throw new ArgumentException($"Unknown tool: {toolName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool execution failed for {ToolName}", toolName);
                throw;
            }
        }

        public Task<object> ListResourcesAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing available resources");
            
            if(_workspace == null) return Task.FromResult(new { } as object) ;

            var solution = _workspace.CurrentSolution;
            var resources = new List<object>();

            // Add solution-level resources
            resources.Add(new
            {
                uri = $"vs://solution",
                mimeType = "application/json",
                description = "Solution information"
            });

            // Add project resources
            foreach (var project in solution.Projects)
            {
                resources.Add(new
                {
                    uri = $"vs://project/{project.Id}",
                    mimeType = "application/json",
                    description = $"Project: {project.Name}"
                });

                // Add key files
                foreach (var document in project.Documents.Take(10))
                {
                    resources.Add(new
                    {
                        uri = $"vs://document/{document.Id}",
                        mimeType = "text/plain",
                        description = $"File: {Path.GetFileName(document.FilePath)}"
                    });
                }
            }

            _logger.LogDebug("Found {ResourceCount} resources", resources.Count);
            return Task.FromResult(new { resources } as object);
        }

        public async Task<object> ReadResourceAsync(string uri, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Reading resource: {Uri}", uri);

            try
            {
                if (uri.StartsWith("vs://solution"))
                {
                    var solution = _workspace.CurrentSolution;
                    var solutionInfo = new
                    {
                        name = Path.GetFileNameWithoutExtension(solution.FilePath),
                        path = solution.FilePath,
                        projects = solution.Projects.Select(p => new
                        {
                            name = p.Name,
                            id = p.Id.ToString(),
                            language = p.Language,
                            documentsCount = p.Documents.Count()
                        }).ToArray()
                    };

                    return new
                    {
                        contents = new[]
                        {
                            new
                            {
                                uri = uri,
                                mimeType = "application/json",
                                text = JsonSerializer.Serialize(solutionInfo, new JsonSerializerOptions { WriteIndented = true })
                            }
                        }
                    };
                }
                else if (uri.StartsWith("vs://project/"))
                {
                    var projectIdStr = uri.Substring("vs://project/".Length);
                    if (Guid.TryParse(projectIdStr, out var projectId))
                    {
                        var project = _workspace.CurrentSolution.GetProject(ProjectId.CreateFromSerialized(projectId));
                        if (project != null)
                        {
                            var projectInfo = new
                            {
                                name = project.Name,
                                language = project.Language,
                                path = project.FilePath,
                                assemblyName = project.AssemblyName,
                                documents = project.Documents.Select(d => new
                                {
                                    name = d.Name,
                                    path = d.FilePath,
                                    id = d.Id.ToString()
                                }).ToArray()
                            };

                            return new
                            {
                                contents = new[]
                                {
                                    new
                                    {
                                        uri = uri,
                                        mimeType = "application/json",
                                        text = JsonSerializer.Serialize(projectInfo, new JsonSerializerOptions { WriteIndented = true })
                                    }
                                }
                            };
                        }
                    }
                }
                else if (uri.StartsWith("vs://document/"))
                {
                    var documentIdStr = uri.Substring("vs://document/".Length);
                    if (Guid.TryParse(documentIdStr, out var documentId))
                    {
                        // Find document in all projects
                        var document = _workspace.CurrentSolution.Projects
                            .SelectMany(p => p.Documents)
                            .FirstOrDefault(d => d.Id.Id == documentId);
                            
                        if (document != null)
                        {
                            var text = await document.GetTextAsync();
                            return new
                            {
                                contents = new[]
                                {
                                    new
                                    {
                                        uri = uri,
                                        mimeType = "text/plain",
                                        text = text.ToString()
                                    }
                                }
                            };
                        }
                    }
                }

                _logger.LogWarning("Resource not found: {Uri}", uri);
                throw new ArgumentException($"Resource not found: {uri}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Resource read failed for {Uri}", uri);
                throw;
            }
        }

        private async Task<object> FindSymbolsAsync(JsonElement arguments)
        {
            var symbolName = arguments.GetProperty("name").GetString();
            var symbolKind = arguments.TryGetProperty("kind", out var kindProp) ? kindProp.GetString() : null;

            _logger.LogDebug("Finding symbols: {SymbolName} (kind: {SymbolKind})", symbolName, symbolKind);

            var solution = _workspace.CurrentSolution;
            var results = new List<object>();

            foreach (var project in solution.Projects)
            {
                var symbols = await SymbolFinder.FindDeclarationsAsync(
                    project,
                    symbolName,
                    ignoreCase: false,
                    CancellationToken.None);

                if (!string.IsNullOrEmpty(symbolKind))
                {
                    symbols = symbols.Where(s => s.Kind.ToString().Equals(symbolKind, StringComparison.OrdinalIgnoreCase));
                }

                foreach (var symbol in symbols)
                {
                    var location = symbol.Locations.FirstOrDefault(l => l.IsInSource);
                    if (location != null)
                    {
                        var lineSpan = location.GetLineSpan();
                        results.Add(new
                        {
                            name = symbol.Name,
                            kind = symbol.Kind.ToString(),
                            filePath = location.SourceTree?.FilePath,
                            line = lineSpan.StartLinePosition.Line + 1,
                            column = lineSpan.StartLinePosition.Character + 1,
                            containingType = symbol.ContainingType?.Name,
                            accessibility = symbol.DeclaredAccessibility.ToString()
                        });
                    }
                }
            }

            _logger.LogDebug("Found {Count} symbols matching {SymbolName}", results.Count, symbolName);

            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            };
        }

        private async Task<object> GetSymbolAtLocationAsync(JsonElement arguments)
        {
            var filePath = arguments.GetProperty("filePath").GetString();
            var line = arguments.GetProperty("line").GetInt32();
            var column = arguments.GetProperty("column").GetInt32();

            _logger.LogDebug("Getting symbol at {FilePath}:{Line}:{Column}", filePath, line, column);

            var solution = _workspace.CurrentSolution;
            var document = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            if (document == null)
            {
                _logger.LogWarning("Document not found: {FilePath}", filePath);
                throw new ArgumentException($"Document not found: {filePath}");
            }

            var semanticModel = await document.GetSemanticModelAsync();
            var syntaxTree = await document.GetSyntaxTreeAsync();
            
            if (semanticModel == null || syntaxTree == null)
            {
                _logger.LogError("Could not get semantic model for {FilePath}", filePath);
                throw new InvalidOperationException("Could not get semantic model");
            }

            var linePosition = new Microsoft.CodeAnalysis.Text.LinePosition(line - 1, column - 1);
            var position = (await syntaxTree.GetTextAsync()).Lines.GetPosition(linePosition);
            
            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(
                semanticModel, 
                position, 
                solution.Workspace);

            if (symbol == null)
            {
                _logger.LogDebug("No symbol found at {FilePath}:{Line}:{Column}", filePath, line, column);
                return new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = "No symbol found at the specified location"
                        }
                    }
                };
            }

            var symbolInfo = new
            {
                name = symbol.Name,
                kind = symbol.Kind.ToString(),
                containingType = symbol.ContainingType?.Name,
                containingNamespace = symbol.ContainingNamespace?.Name,
                documentation = symbol.GetDocumentationCommentXml(),
                signature = symbol.ToDisplayString(),
                accessibility = symbol.DeclaredAccessibility.ToString()
            };

            _logger.LogDebug("Found symbol: {SymbolName} ({SymbolKind})", symbol.Name, symbol.Kind);

            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(symbolInfo, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            };
        }

        private async Task<object> GetSolutionProjectsAsync(JsonElement arguments)
        {
            _logger.LogDebug("Getting solution projects");
            
            var solution = _workspace.CurrentSolution;
            var projects = new List<object>();

            foreach (var project in solution.Projects)
            {
                // Get more detailed project information
                var compilation = await project.GetCompilationAsync();
                
                projects.Add(new
                {
                    id = project.Id.ToString(),
                    name = project.Name,
                    language = project.Language,
                    filePath = project.FilePath,
                    assemblyName = project.AssemblyName,
                    outputFilePath = project.OutputFilePath,
                    documentsCount = project.Documents.Count(),
                    analysisReferences = project.AnalyzerReferences.Count(),
                    projectReferences = project.ProjectReferences.Count(),
                    metadataReferences = project.MetadataReferences.Count(),
                    compilationOptions = compilation?.Options?.GetType().Name
                });
            }

            _logger.LogInformation("Solution has {ProjectCount} projects", projects.Count);

            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            };
        }

        private async Task<object> GetDocumentOutlineAsync(JsonElement arguments)
        {
            var filePath = arguments.GetProperty("filePath").GetString();
            
            _logger.LogDebug("Getting document outline for {FilePath}", filePath);

            var solution = _workspace.CurrentSolution;
            var document = solution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

            if (document == null)
            {
                _logger.LogWarning("Document not found: {FilePath}", filePath);
                throw new ArgumentException($"Document not found: {filePath}");
            }

            var semanticModel = await document.GetSemanticModelAsync();
            var syntaxTree = await document.GetSyntaxTreeAsync();
            
            if (semanticModel == null || syntaxTree == null)
            {
                _logger.LogError("Could not get semantic model for {FilePath}", filePath);
                throw new InvalidOperationException("Could not get semantic model");
            }

            var root = await syntaxTree.GetRootAsync();
            var symbols = new List<object>();

            // Find all symbols in the document
            foreach (var node in root.DescendantNodes())
            {
                var symbol = semanticModel.GetDeclaredSymbol(node);
                if (symbol != null && symbol.Locations.Any(l => l.IsInSource))
                {
                    var location = symbol.Locations.First(l => l.IsInSource);
                    var lineSpan = location.GetLineSpan();
                    
                    symbols.Add(new
                    {
                        name = symbol.Name,
                        kind = symbol.Kind.ToString(),
                        line = lineSpan.StartLinePosition.Line + 1,
                        column = lineSpan.StartLinePosition.Character + 1,
                        endLine = lineSpan.EndLinePosition.Line + 1,
                        endColumn = lineSpan.EndLinePosition.Character + 1,
                        accessibility = symbol.DeclaredAccessibility.ToString(),
                        containingType = symbol.ContainingType?.Name
                    });
                }
            }

            _logger.LogDebug("Found {Count} symbols in document {FilePath}", symbols.Count, filePath);

            return new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = JsonSerializer.Serialize(symbols.OrderBy(s => ((JsonElement)s).GetProperty("line").GetInt32()), 
                                                      new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            };
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }
    }
}
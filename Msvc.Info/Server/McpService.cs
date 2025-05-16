
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Msvc.Info.Core.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Msvc.Info.Server
{
    /// <summary>
    /// MCP Service implementation with Visual Studio integration
    /// </summary>
    [VisualStudioContribution]
    internal class MCPService : IMCPService
    {
        private readonly VisualStudioWorkspace? _workspace;
        private readonly ILogger<MCPService> _logger;
        private readonly IPathTranslationService _pathTranslationService;
        private IHttpServer? _httpServer = null;
        private readonly IServiceProvider _sp;
        private bool disposedValue;

        public MCPService(
            ILogger<MCPService> logger,
            IPathTranslationService pathTranslationService,
            IServiceProvider sp)
        {
            _logger = logger;
            _pathTranslationService = pathTranslationService;
            _sp = sp;
            
            // Get Visual Studio workspace through Service Provider
            _workspace = ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var componentModel = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
                return componentModel?.GetService<VisualStudioWorkspace>();
            });

            _logger.LogInformation("MCP Service initialized");
        }

        [VisualStudioContribution]
        public static BrokeredServiceConfiguration BrokeredServiceConfiguration
            => new(IMCPService.Configuration.ServiceName, IMCPService.Configuration.ServiceVersion, typeof(MCPService))
            {
                ServiceAudience = BrokeredServiceAudience.Local | BrokeredServiceAudience.Public,
            };

        public Task<object> InitializeAsync(object parameters, CancellationToken cancellationToken = default)
        {
            if(_httpServer == null)
            {
                _httpServer = _sp.GetService<IHttpServer>();
                // connect MCPHttpServer to this service
                if (_httpServer is MCPHttpServer httpServer)
                {
                    httpServer._mcpService = this;
                }
            }

            _logger.LogInformation("MCP Service initialized by client");
            
            // Start HTTP server if available
            if (_httpServer != null && !_httpServer.IsRunning)
            {
                try
                {
                    _httpServer.Start();
                    _logger.LogInformation("HTTP server started on {BaseUrl}", _httpServer.BaseUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start HTTP server");
                    // Don't fail initialization if HTTP server fails to start
                }
            }
            
            return Task.FromResult(new
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
                    name = "VisualStudio MCP Service",
                    version = "1.0.0"
                }
            } as object);
        }

        public Task<object> ListToolsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing available tools");
            
            var tools = new[]
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
                },
                new ToolDefinition
                {
                    name = "translate_path",
                    description = "Translate a path between Windows and WSL formats",
                    inputSchema = new InputSchema
                    {
                        type = "object",
                        properties = new Dictionary<string, SchemaProperty>
                        {
                            ["path"] = new SchemaProperty { type = "string", description = "The path to translate" },
                            ["sourceFormat"] = new SchemaProperty { type = "string", description = "Source format: 'Windows', 'WSL', or 'URI'" },
                            ["targetFormat"] = new SchemaProperty { type = "string", description = "Target format: 'Windows', 'WSL', or 'URI'" }
                        },
                        required = new[] { "path", "sourceFormat", "targetFormat" }
                    }
                }
            };

            return Task.FromResult(new { tools } as object);
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
                    
                    case "translate_path":
                        return await TranslatePathAsync(arguments);
                    
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
            
            if(_workspace == null) return Task.FromResult(new { } as object);

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
                if (uri.StartsWith("vs://solution") && _workspace != null)
                {
                    var solution = _workspace.CurrentSolution;
                    
                    // Translate paths to WSL format for AI tools
                    var solutionPath = _pathTranslationService.TranslatePath(solution.FilePath, PathFormat.Windows, PathFormat.Wsl);
                    
                    var solutionInfo = new
                    {
                        name = Path.GetFileNameWithoutExtension(solution.FilePath),
                        path = solutionPath,
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
                                uri,
                                mimeType = "application/json",
                                text = JsonSerializer.Serialize(solutionInfo, new JsonSerializerOptions { WriteIndented = true })
                            }
                        }
                    };
                }
                else if (uri.StartsWith("vs://project/") && _workspace != null)
                {
                    var projectIdStr = uri.Substring("vs://project/".Length);
                    if (Guid.TryParse(projectIdStr, out var projectId))
                    {
                        var project = _workspace.CurrentSolution.GetProject(ProjectId.CreateFromSerialized(projectId));
                        if (project != null)
                        {
                            // Translate project path to WSL format
                            var projectPath = _pathTranslationService.TranslatePath(project.FilePath, PathFormat.Windows, PathFormat.Wsl);
                            
                            var projectInfo = new
                            {
                                name = project.Name,
                                language = project.Language,
                                path = projectPath,
                                assemblyName = project.AssemblyName,
                                documents = project.Documents.Select(d => new
                                {
                                    name = d.Name,
                                    path = _pathTranslationService.TranslatePath(d.FilePath, PathFormat.Windows, PathFormat.Wsl),
                                    id = d.Id.ToString()
                                }).ToArray()
                            };

                            return new
                            {
                                contents = new[]
                                {
                                    new
                                    {
                                        uri,
                                        mimeType = "application/json",
                                        text = JsonSerializer.Serialize(projectInfo, new JsonSerializerOptions { WriteIndented = true })
                                    }
                                }
                            };
                        }
                    }
                }
                else if (uri.StartsWith("vs://document/") && _workspace != null)
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
                                        uri,
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

            if(_workspace == null)  
            {
                _logger.LogWarning("missing workspace");
                return new { content = new[] { new { type = "text", text = "Workspace not available" } } };
            }

            var solution = _workspace.CurrentSolution;
            var results = new List<object>();

            foreach (var project in solution.Projects)
            {
                var symbols = await SymbolFinder.FindDeclarationsAsync(
                    project,
                    symbolName ?? "unkn",
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

            if (_workspace == null)
            {
                _logger.LogWarning("missing workspace");
                return new { content = new[] { new { type = "text", text = "Workspace not available" } } };
            }

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

            if (_workspace == null)
            {
                _logger.LogWarning("missing workspace");
                return new { content = new[] { new { type = "text", text = "Workspace not available" } } };
            }

            var solution = _workspace.CurrentSolution;
            var projects = new List<object>();

            foreach (var project in solution.Projects)
            {
                // Get more detailed project information
                var compilation = await project.GetCompilationAsync();
                
                // Translate paths to WSL format for AI tools
                var projectPath = _pathTranslationService.TranslatePath(project.FilePath, PathFormat.Windows, PathFormat.Wsl);
                var outputPath = _pathTranslationService.TranslatePath(project.OutputFilePath, PathFormat.Windows, PathFormat.Wsl);
                
                projects.Add(new
                {
                    id = project.Id.ToString(),
                    name = project.Name,
                    language = project.Language,
                    filePath = projectPath,
                    assemblyName = project.AssemblyName,
                    outputFilePath = outputPath,
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

            if (_workspace == null)
            {
                _logger.LogWarning("missing workspace");
                return new { content = new[] { new { type = "text", text = "Workspace not available" } } };
            }

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
                        text = JsonSerializer.Serialize(symbols.OrderBy(s => ((dynamic)s).line), 
                                                      new JsonSerializerOptions { WriteIndented = true })
                    }
                }
            };
        }

        private Task<object> TranslatePathAsync(JsonElement arguments)
        {
            try
            {
                var path = arguments.GetProperty("path").GetString();
                var sourceFormatStr = arguments.GetProperty("sourceFormat").GetString();
                var targetFormatStr = arguments.GetProperty("targetFormat").GetString();

                if (string.IsNullOrEmpty(path))
                {
                    throw new ArgumentNullException(nameof(path), "Path cannot be null or empty");
                }

                if (!Enum.TryParse<PathFormat>(sourceFormatStr, true, out var sourceFormat))
                {
                    throw new ArgumentException($"Invalid source format: {sourceFormatStr}. Valid values are: Windows, Wsl, Uri, Auto");
                }

                if (!Enum.TryParse<PathFormat>(targetFormatStr, true, out var targetFormat))
                {
                    throw new ArgumentException($"Invalid target format: {targetFormatStr}. Valid values are: Windows, Wsl, Uri");
                }

                _logger.LogDebug("Translating path {Path} from {SourceFormat} to {TargetFormat}", path, sourceFormat, targetFormat);

                var translatedPath = _pathTranslationService.TranslatePath(path, sourceFormat, targetFormat);

                if (string.IsNullOrEmpty(translatedPath))
                {
                    throw new InvalidOperationException($"Failed to translate path: {path}");
                }

                return Task.FromResult(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = translatedPath
                        }
                    }
                } as object);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Path translation failed");
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Stop HTTP server if it's running
                    if (_httpServer != null && _httpServer.IsRunning)
                    {
                        try
                        {
                            _httpServer.StopAsync().Wait(TimeSpan.FromSeconds(5));
                            _logger.LogInformation("HTTP server stopped");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error stopping HTTP server");
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MCPService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

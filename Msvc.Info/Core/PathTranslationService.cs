using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Zerox.Msvc.Core.Services
{

    /// <summary>
    /// Implementation of IPathTranslationService for bidirectional path translation.
    /// </summary>
    public class PathTranslationService : IPathTranslationService
    {
        private readonly ILogger<PathTranslationService> _logger;
        private static readonly Regex _wslPathRegex = new Regex(@"^/mnt/([a-z])/(.*)$", RegexOptions.Compiled);
        private static readonly Regex _windowsPathRegex = new Regex(@"^([A-Za-z]):\\(.*)$", RegexOptions.Compiled);
        private static readonly Regex _uriPathRegex = new Regex(@"^file:///(.*)$", RegexOptions.Compiled);
        
        public PathTranslationService(ILogger<PathTranslationService> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Creates a composite project identifier using project name and relative path
        /// </summary>
        /// <param name="projectName">The name of the project</param>
        /// <param name="projectPath">The full path to the project file</param>
        /// <param name="solutionPath">The full path to the solution file</param>
        /// <returns>A unique composite identifier (name__relativePath)</returns>
        /// <exception cref="ArgumentException">Thrown when project name is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when creating the ID fails</exception>
        public string? CreateCompositeProjectId(string? projectName, string? projectPath, string solutionPath)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                throw new ArgumentException("Project name cannot be null or empty", nameof(projectName));
            }
            
            if (string.IsNullOrEmpty(projectPath))
            {
                throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));
            }
            
            if (string.IsNullOrEmpty(solutionPath))
            {
                throw new ArgumentException("Solution path cannot be null or empty", nameof(solutionPath));
            }
            
            // Get the relative path from solution to project
            // Let exceptions from GetRelativePath bubble up
            var relativePath = GetRelativePath(solutionPath, projectPath);
            
            if (string.IsNullOrEmpty(relativePath))
            {
                throw new InvalidOperationException($"Failed to get relative path from solution to project: {projectName}");
            }

            // Replace characters that would be problematic in URLs
            var safeRelativePath = relativePath?
                .Replace('\\', '_')
                .Replace('/', '_')
                .Replace(' ', '_')
                .Replace('.', '_');

            // Combine to create a unique identifier
            return $"{projectName}__{safeRelativePath}";
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when path format is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when path translation fails</exception>
        public string? TranslatePath(string? path, PathFormat sourceFormat, PathFormat targetFormat)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (sourceFormat == targetFormat)
            {
                return path;
            }

            // Auto-detect source format if needed
            if (sourceFormat == PathFormat.Auto)
            {
                sourceFormat = DetectPathFormat(path);
                _logger.LogDebug($"Auto-detected path format as {sourceFormat} for path: {path}");
            }

            // First convert to Windows format as the intermediate format
            string? windowsPath;
            try 
            {
                windowsPath = sourceFormat switch
                {
                    PathFormat.Windows => path,
                    PathFormat.Wsl => WslToWindows(path),
                    PathFormat.Uri => UriToWindows(path),
                    _ => throw new ArgumentException($"Unsupported source path format: {sourceFormat}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error converting path from {sourceFormat} to Windows format: {path}");
                throw new InvalidOperationException($"Failed to convert path from {sourceFormat} format: {path}", ex);
            }

            // Then convert from Windows to the target format
            string? result;
            try
            {
                result = targetFormat switch
                {
                    PathFormat.Windows => windowsPath,
                    PathFormat.Wsl => WindowsToWsl(windowsPath),
                    PathFormat.Uri => WindowsToUri(windowsPath),
                    _ => throw new ArgumentException($"Unsupported target path format: {targetFormat}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error converting path from Windows format to {targetFormat}: {windowsPath}");
                throw new InvalidOperationException($"Failed to convert path to {targetFormat} format: {windowsPath}", ex);
            }

            _logger.LogDebug($"Translated path from {sourceFormat} to {targetFormat}: {path} -> {result}");
            return result;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">Thrown when path format is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when path processing fails</exception>
        public string? GetRelativePath(string? basePath, string? fullPath, PathFormat targetFormat = PathFormat.Windows)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));
            }
            
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException("Full path cannot be null or empty", nameof(fullPath));
            }

            // Detect formats if needed
            var basePathFormat = DetectPathFormat(basePath);
            var fullPathFormat = DetectPathFormat(fullPath);
            _logger.LogDebug($"Detected formats - basePath: {basePathFormat}, fullPath: {fullPathFormat}");

            // Ensure both paths are in Windows format for processing
            // Let exceptions from TranslatePath bubble up
            var windowsBasePath = basePathFormat == PathFormat.Windows 
                ? basePath 
                : TranslatePath(basePath, basePathFormat, PathFormat.Windows);
            
            var windowsFullPath = fullPathFormat == PathFormat.Windows 
                ? fullPath 
                : TranslatePath(fullPath, fullPathFormat, PathFormat.Windows);

            // Get the base directory
            string? baseDir = Path.GetDirectoryName(windowsBasePath);
            if (string.IsNullOrEmpty(baseDir))
            {
                _logger.LogWarning($"Could not get directory name from base path: {windowsBasePath}");
                // Let exceptions from TranslatePath bubble up
                return TranslatePath(fullPath, PathFormat.Windows, targetFormat);
            }

            // Calculate the relative path
            string? relativePath;
            if (windowsFullPath!= null && windowsFullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = windowsFullPath.Substring(baseDir.Length).TrimStart('\\', '/');
            }
            else
            {
                // Paths don't share a common base, use full path
                relativePath = windowsFullPath;
            }

            // Convert to the target format
            string? result;
            try
            {
                result = targetFormat switch
                {
                    PathFormat.Windows => relativePath?.Replace('/', '\\'),
                    PathFormat.Wsl => relativePath?.Replace('\\', '/'),
                    PathFormat.Uri => $"file:///{relativePath?.Replace('\\', '/')}",
                    _ => throw new ArgumentException($"Unsupported target path format: {targetFormat}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error formatting relative path to {targetFormat} format: {relativePath}");
                throw new InvalidOperationException($"Failed to format relative path to {targetFormat} format", ex);
            }

            _logger.LogDebug($"Calculated relative path: {result} (base: {basePath}, full: {fullPath})");
            return result;
        }

        /// <summary>
        /// Detects the format of a path based on its structure.
        /// </summary>
        /// <param name="path">The path to analyze</param>
        /// <returns>The detected path format</returns>
        private PathFormat DetectPathFormat(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return PathFormat.Windows; // Default
            }

            if (_windowsPathRegex.IsMatch(path))
            {
                return PathFormat.Windows;
            }

            if (_wslPathRegex.IsMatch(path))
            {
                return PathFormat.Wsl;
            }

            if (_uriPathRegex.IsMatch(path))
            {
                return PathFormat.Uri;
            }

            // If no clear pattern is matched, make a best guess
            if (path?.Contains('\\') ?? false)
            {
                return PathFormat.Windows;
            }
            
            if (path?.StartsWith("/") ?? false)
            {
                return PathFormat.Wsl;
            }

            return PathFormat.Windows; // Default to Windows
        }

        /// <summary>
        /// Converts a WSL path to Windows format
        /// </summary>
        /// <param name="wslPath">The WSL path (e.g., /mnt/c/path/to/file.cs)</param>
        /// <returns>The Windows path (e.g., C:\path\to\file.cs)</returns>
        private string? WslToWindows(string? wslPath)
        {
            if (string.IsNullOrEmpty(wslPath))
            {
                return wslPath;
            }

            var match = _wslPathRegex.Match(wslPath);
            if (match.Success)
            {
                var drive = match.Groups[1].Value.ToUpper();
                var path = match.Groups[2].Value.Replace('/', '\\');
                return $"{drive}:\\{path}";
            }

            // If the path doesn't match the WSL format, return as is
            return wslPath;
        }

        /// <summary>
        /// Converts a Windows path to WSL format
        /// </summary>
        /// <param name="windowsPath">The Windows path (e.g., C:\path\to\file.cs)</param>
        /// <returns>The WSL path (e.g., /mnt/c/path/to/file.cs)</returns>
        private string? WindowsToWsl(string? windowsPath)
        {
            if (string.IsNullOrEmpty(windowsPath))
            {
                return windowsPath;
            }

            var match = _windowsPathRegex.Match(windowsPath);
            if (match.Success)
            {
                var drive = match.Groups[1].Value.ToLower();
                var path = match.Groups[2].Value.Replace('\\', '/');
                return $"/mnt/{drive}/{path}";
            }

            // If the path doesn't match the Windows format, return as is
            return windowsPath;
        }

        /// <summary>
        /// Converts a URI path to Windows format
        /// </summary>
        /// <param name="uriPath">The URI path (e.g., file:///C:/path/to/file.cs)</param>
        /// <returns>The Windows path (e.g., C:\path\to\file.cs)</returns>
        private string? UriToWindows(string? uriPath)
        {
            if (string.IsNullOrEmpty(uriPath))
            {
                return uriPath;
            }

            var match = _uriPathRegex.Match(uriPath);
            if (!match.Success)
            {
                return uriPath;
            }

            var path = match.Groups[1].Value;

            // Check if it's a WSL-style path in a URI
            if (path.StartsWith("mnt/"))
            {
                // Remove the "mnt/" prefix and treat as WSL path
                return WslToWindows($"/{path}");
            }

            // For Windows paths with drive letter
            if (path.Length > 2 && path[1] == ':')
            {
                return path.Replace('/', '\\');
            }

            // For other paths, just replace slashes
            return path.Replace('/', '\\');
        }

        /// <summary>
        /// Converts a Windows path to URI format
        /// </summary>
        /// <param name="windowsPath">The Windows path (e.g., C:\path\to\file.cs)</param>
        /// <returns>The URI path (e.g., file:///C:/path/to/file.cs)</returns>
        private string? WindowsToUri(string? windowsPath)
        {
            if (string.IsNullOrEmpty(windowsPath))
            {
                return windowsPath;
            }

            // Convert backslashes to forward slashes
            var uriPath = windowsPath?.Replace('\\', '/');

            // Ensure it starts with file:///
            if (uriPath?.Length > 2 && uriPath[1] == ':')
            {
                return $"file:///{uriPath}";
            }

            return $"file:///{uriPath}";
        }
    }
}
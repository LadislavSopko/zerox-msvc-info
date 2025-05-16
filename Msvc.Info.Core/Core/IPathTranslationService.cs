namespace Msvc.Info.Core.Services
{
    /// <summary>
    /// Supported path formats for translation.
    /// </summary>
    public enum PathFormat
    {
        /// <summary>
        /// Auto-detect the path format based on path structure.
        /// </summary>
        Auto,

        /// <summary>
        /// Windows path format (e.g., C:\Path\To\File.cs).
        /// </summary>
        Windows,

        /// <summary>
        /// WSL path format (e.g., /mnt/c/Path/To/File.cs).
        /// </summary>
        Wsl,

        /// <summary>
        /// URI path format (e.g., file:///C:/Path/To/File.cs).
        /// </summary>
        Uri
    }

    /// <summary>
    /// Service for translating paths between different formats.
    /// Provides bidirectional conversion between Windows and WSL paths.
    /// </summary>
    public interface IPathTranslationService
    {
        /// <summary>
        /// Translates a path from one format to another.
        /// </summary>
        /// <param name="path">The path to translate.</param>
        /// <param name="sourceFormat">The source format of the path.</param>
        /// <param name="targetFormat">The target format for the translation.</param>
        /// <returns>The translated path.</returns>
        string? TranslatePath(string? path, PathFormat sourceFormat, PathFormat targetFormat);

        /// <summary>
        /// Gets a path relative to a base path.
        /// </summary>
        /// <param name="basePath">The base path.</param>
        /// <param name="fullPath">The full path to convert to relative.</param>
        /// <param name="targetFormat">The format for the output path.</param>
        /// <returns>The relative path.</returns>
        string? GetRelativePath(string? basePath, string? fullPath, PathFormat targetFormat = PathFormat.Windows);
        
        /// <summary>
        /// Creates a composite project identifier using project name and relative path.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="projectPath">The full path to the project file.</param>
        /// <param name="solutionPath">The full path to the solution file.</param>
        /// <returns>A unique composite identifier (name__relativePath).</returns>
        string? CreateCompositeProjectId(string? projectName, string? projectPath, string solutionPath);
    }
}
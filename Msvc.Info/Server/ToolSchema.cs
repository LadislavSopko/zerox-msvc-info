namespace Msvc.Info.Server
{
    /// <summary>
    /// Represents a property in a tool's schema
    /// </summary>
    public class SchemaProperty
    {
        public string type { get; set; } = "string";
        public string description { get; set; } = "";
    }

    /// <summary>
    /// Represents a tool's input schema
    /// </summary>
    public class InputSchema
    {
        public string type { get; set; } = "object";
        public Dictionary<string, SchemaProperty> properties { get; set; } = new Dictionary<string, SchemaProperty>();
        public string[] required { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Represents a tool definition
    /// </summary>
    public class ToolDefinition
    {
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public InputSchema inputSchema { get; set; } = new InputSchema();
    }
}

using ModelContextProtocol.Server;
using System.ComponentModel;

namespace TiaMcpServer.ModelContextProtocol
{
    [McpServerPromptType]
    public static partial class McpPrompts
    {
        // Optional prompt arguments arrive as empty text when the user leaves them out; only real
        // values are passed on to the tool, so the tool's own default applies otherwise.
        private static string OptionalParameter(string name, string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : $"\n- {name}: {value!.Trim()}";
        }

        // MCP prompt arguments are always strings, so boolean flags arrive as text.
        private static string NormalizeBool(string? value)
        {
            // An explicit null check: on net48 string.IsNullOrEmpty has no [NotNullWhen] annotation
            var text = value?.Trim();
            if (text is null || text.Length == 0)
            {
                return "false";
            }

            return text.Equals("true", System.StringComparison.OrdinalIgnoreCase)
                || text.Equals("1", System.StringComparison.Ordinal)
                || text.Equals("yes", System.StringComparison.OrdinalIgnoreCase)
                    ? "true"
                    : "false";
        }
    }
}

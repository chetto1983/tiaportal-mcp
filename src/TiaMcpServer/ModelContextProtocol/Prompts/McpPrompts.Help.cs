using ModelContextProtocol.Server;
using System.ComponentModel;

namespace TiaMcpServer.ModelContextProtocol
{
    public static partial class McpPrompts
    {
        #region TIA Portal help

        [McpServerPrompt(Name = "SearchHelp"), Description("Search the installed TIA Portal help")]
        public static string SearchHelp(string searchText, string device = "")
        {
            return $@"Search the installed TIA Portal help (F1 help) and list the matching topics. Read a topic with 'GetHelpTopic'.

Common parameter values:
- searchText: words to search for, e.g. an instruction name
- device: device family from 'GetHelpFilters', e.g. S7-1200; empty searches everything

Use the SearchHelp tool with these parameters:
- searchText: {searchText}
- device: {device}";
        }

        [McpServerPrompt(Name = "GetHelpTopic"), Description("Read one TIA Portal help topic as text")]
        public static string GetHelpTopic(string topicId)
        {
            return $@"Read one TIA Portal help topic as plain text. Follow its links with further 'GetHelpTopic' calls.

Common parameter values:
- topicId: topic id from 'SearchHelp' or from the links of another topic

Use the GetHelpTopic tool with these parameters:
- topicId: {topicId}";
        }

        [McpServerPrompt(Name = "GetHelpFilters"), Description("List the device families of the TIA Portal help")]
        public static string GetHelpFilters()
        {
            return @"List the device families the TIA Portal help can be filtered by.

Use the GetHelpFilters tool (it takes no parameters).";
        }

        #endregion
    }
}

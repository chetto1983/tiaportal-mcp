using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Text.Json.Nodes;
using TiaMcpServer.Siemens;

namespace TiaMcpServer.ModelContextProtocol
{
    public static partial class McpServer
    {
        // The TIA Portal F1 help, read through the local TIA Help Viewer service.
        //
        // Callers: registered through Program.BuildTools(). Data: ResponseHelpSearch, ResponseHelpTopic
        // and ResponseHelpFilters as structuredContent. The tools need neither TIA Portal nor a
        // project, only the help service, so they are [PortalIndependent].

        [PortalIndependent]
        [McpServerTool(Name = "SearchHelp", Title = "Search TIA Portal help", ReadOnly = true, OpenWorld = true, UseStructuredContent = true),
         Description("Search the installed TIA Portal help (the F1 help: instructions, technology objects, hardware, Openness) and return the matching topics with their 'topicId' for 'GetHelpTopic'")]
        public static ResponseHelpSearch SearchHelp(
            [Description("searchText: words to search for, e.g. 'high speed counter' or 'CTRL_HSC_EXT'")] string searchText,
            [Description("device: device family to restrict the search to, e.g. 'S7-1200' (see 'GetHelpFilters'); empty searches everything")] string device = "")
        {
            return Unguarded(nameof(SearchHelp), () =>
            {
                var results = HelpViewer.Search(searchText, device);
                return new ResponseHelpSearch
                {
                    Message = $"{results.Count} help topic(s) found for '{searchText}'",
                    Results = results,
                    Meta = HelpMeta()
                };
            });
        }

        [PortalIndependent]
        [McpServerTool(Name = "GetHelpTopic", Title = "Get TIA Portal help topic", ReadOnly = true, OpenWorld = true, UseStructuredContent = true),
         Description("Read one TIA Portal help topic as plain text, with its manual, device family, parent topic and the topics it links to")]
        public static ResponseHelpTopic GetHelpTopic(
            [Description("topicId: topic id from 'SearchHelp' or from the links of another topic, e.g. 'ProgCPU12002MenUS/11627607179/12970745995.htm'")] string topicId)
        {
            return Unguarded(nameof(GetHelpTopic), () =>
            {
                var topic = HelpViewer.GetTopic(topicId);
                return new ResponseHelpTopic
                {
                    Message = $"Help topic '{topic.Title}' ({topic.Text.Length} characters, {topic.Links.Count} link(s))",
                    Topic = topic,
                    Meta = HelpMeta()
                };
            });
        }

        [PortalIndependent]
        [McpServerTool(Name = "GetHelpFilters", Title = "Get TIA Portal help filters", ReadOnly = true, OpenWorld = true, UseStructuredContent = true),
         Description("List the device families the TIA Portal help can be filtered by; pass a 'device' value to 'SearchHelp'")]
        public static ResponseHelpFilters GetHelpFilters()
        {
            return Unguarded(nameof(GetHelpFilters), () =>
            {
                var devices = HelpViewer.GetDeviceFilters();
                return new ResponseHelpFilters
                {
                    Message = $"{devices.Count} device filter(s)",
                    Devices = devices,
                    Meta = HelpMeta()
                };
            });
        }

        private static JsonObject HelpMeta() => new JsonObject
        {
            ["timestamp"] = DateTime.Now,
            ["success"] = true,
            ["helpUrl"] = HelpViewer.EffectiveBaseUrl
        };
    }
}

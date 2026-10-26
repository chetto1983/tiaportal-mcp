using System.Collections.Generic;
using TiaMcpServer.Siemens;

namespace TiaMcpServer.ModelContextProtocol
{
    public class ResponseHelpSearch : ResponseMessage
    {
        public List<HelpViewer.SearchResult>? Results { get; set; }
    }

    /// <summary>One help topic as plain text, with the topics it links to.</summary>
    public class ResponseHelpTopic : ResponseMessage
    {
        public HelpViewer.Topic? Topic { get; set; }
    }

    public class ResponseHelpFilters : ResponseMessage
    {
        /// <summary>Values accepted by the 'device' argument of SearchHelp.</summary>
        public List<HelpViewer.DeviceFilter>? Devices { get; set; }
    }
}

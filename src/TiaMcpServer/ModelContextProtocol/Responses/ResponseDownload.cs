using System.Collections.Generic;

namespace TiaMcpServer.ModelContextProtocol
{
    /// <summary>Result of a download, with every load-preview question and the answer the server gave.</summary>
    public class ResponseDownload : ResponseMessage
    {
        /// <summary>DownloadResultState: Success, Information, Warning or Error.</summary>
        public string? State { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }

        /// <summary>The download messages of TIA Portal, nested ones indented.</summary>
        public List<string>? Messages { get; set; }

        /// <summary>"&lt;question type&gt;: &lt;answer&gt; (&lt;TIA Portal text&gt;)" for each load-preview question.</summary>
        public List<string>? Decisions { get; set; }
    }
}

using System.Collections.Generic;

namespace TiaMcpServer.ModelContextProtocol
{
    /// <summary>Online state of a PLC and the connections TIA Portal offers to reach it.</summary>
    public class ResponseOnlineState : ResponseMessage
    {
        /// <summary>OnlineState: Offline, Online, Connecting, Disconnecting, NotReachable, Protected, Incompatible.</summary>
        public string? State { get; set; }

        /// <summary>True when TIA Portal already has a connection set for this PLC, so GoOnline needs no mode.</summary>
        public bool IsConfigured { get; set; }

        public List<ResponseConnectionTarget>? Targets { get; set; }
    }

    /// <summary>One way to reach the PLC; pass its fields to GoOnline or DownloadToDevice.</summary>
    public class ResponseConnectionTarget
    {
        public string? Mode { get; set; }
        public string? PcInterface { get; set; }
        public int PcInterfaceNumber { get; set; }
        public string? TargetInterface { get; set; }
    }
}

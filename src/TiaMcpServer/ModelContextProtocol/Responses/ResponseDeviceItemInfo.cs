using System.Collections.Generic;

namespace TiaMcpServer.ModelContextProtocol
{
    public class ResponseDeviceItemInfo : ResponseAttributes
    {
        public string? Name { get; set; }
        public string? Description { get; set; }

        /// <summary>I/O address areas of the item (e.g. the HSC input area of an S7-1200 G2 CPU).</summary>
        public List<ResponseAddress>? Addresses { get; set; }

        /// <summary>Channels of the item with their attributes; empty when Openness exposes none.</summary>
        public List<ResponseChannel>? Channels { get; set; }
    }

    public class ResponseAddress
    {
        /// <summary>Input or Output.</summary>
        public string? IoType { get; set; }
        public int StartAddress { get; set; }

        /// <summary>Length in bits.</summary>
        public int Length { get; set; }
    }

    public class ResponseChannel
    {
        public string? IoType { get; set; }
        public string? Type { get; set; }
        public int Number { get; set; }
        public IEnumerable<Attribute>? Attributes { get; set; }
    }
}

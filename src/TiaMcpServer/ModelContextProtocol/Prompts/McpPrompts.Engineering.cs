using ModelContextProtocol.Server;
using System.ComponentModel;

namespace TiaMcpServer.ModelContextProtocol
{
    public static partial class McpPrompts
    {
        #region Online and download

        [McpServerPrompt(Name = "GetOnlineState"), Description("Show the online state and connections of a PLC")]
        public static string GetOnlineState(string softwarePath)
        {
            return $@"Show whether TIA Portal is online with a PLC and list the connections that GoOnline and DownloadToDevice accept.

Common parameter values:
- softwarePath: defines the path in the project structure to the PLC software

Use the GetOnlineState tool with these parameters:
- softwarePath: {softwarePath}";
        }

        [McpServerPrompt(Name = "GoOnline"), Description("Go online with a PLC")]
        public static string GoOnline(string softwarePath, string mode = "", string pcInterface = "", string pcInterfaceNumber = "1", string targetInterface = "")
        {
            return $@"Connect TIA Portal online to the PLC. While online, Openness refuses to edit the project: go offline before writing.

Common parameter values:
- softwarePath: defines the path in the project structure to the PLC software
- mode, pcInterface, pcInterfaceNumber, targetInterface: a connection from 'GetOnlineState'; empty mode uses the configured connection

Use the GoOnline tool with these parameters:
- softwarePath: {softwarePath}
- mode: {mode}
- pcInterface: {pcInterface}
- pcInterfaceNumber: {pcInterfaceNumber}
- targetInterface: {targetInterface}";
        }

        [McpServerPrompt(Name = "GoOffline"), Description("Go offline from a PLC")]
        public static string GoOffline(string softwarePath)
        {
            return $@"Disconnect TIA Portal from the PLC, e.g. after 'not supported in online mode'.

Common parameter values:
- softwarePath: defines the path in the project structure to the PLC software

Use the GoOffline tool with these parameters:
- softwarePath: {softwarePath}";
        }

        [McpServerPrompt(Name = "DownloadToDevice"), Description("Download hardware and software to a PLC or PLCSIM")]
        public static string DownloadToDevice(string softwarePath, string mode, string pcInterface, string pcInterfaceNumber = "1", string targetInterface = "",
            string options = "HardwareAndSoftware", string allowStop = "false", string reinitialize = "false", string acceptDifferentConfiguration = "false")
        {
            return $@"Download to a PLC or a PLCSIM instance and report every load-preview question with the answer given (requires the server started with '--allow-write'). A module reset is never chosen; stopping the CPU, reinitializing data and accepting a different configuration need the matching flag.

Common parameter values:
- softwarePath: defines the path in the project structure to the PLC software
- mode, pcInterface, pcInterfaceNumber, targetInterface: a connection from 'GetOnlineState'
- options: HardwareAndSoftware (default), Software, SoftwareOnlyChanges or Hardware

Use the DownloadToDevice tool with these parameters:
- softwarePath: {softwarePath}
- mode: {mode}
- pcInterface: {pcInterface}
- pcInterfaceNumber: {pcInterfaceNumber}
- targetInterface: {targetInterface}
- options: {options}
- allowStop: {NormalizeBool(allowStop)}
- reinitialize: {NormalizeBool(reinitialize)}
- acceptDifferentConfiguration: {NormalizeBool(acceptDifferentConfiguration)}";
        }

        #endregion
    }
}

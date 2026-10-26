using ModelContextProtocol.Server;
using System.ComponentModel;

namespace TiaMcpServer.ModelContextProtocol
{
    public static partial class McpPrompts
    {
        #region Online, download, attributes and OBs

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

        [McpServerPrompt(Name = "SetDeviceItemAttribute"), Description("Set one hardware parameter of a device item")]
        public static string SetDeviceItemAttribute(string deviceItemPath, string attributeName, string value)
        {
            return $@"Set one writable hardware parameter of a device item (requires the server started with '--allow-write'). Some attributes appear only after another one is set: re-read the item with 'GetDeviceItemInfo'.

Common parameter values:
- deviceItemPath: defines the path in the project structure to the device item
- attributeName: attribute name as listed by 'GetDeviceItemInfo' (case-sensitive)
- value: new value as text; enums take a name or a number, booleans true/false

Use the SetDeviceItemAttribute tool with these parameters:
- deviceItemPath: {deviceItemPath}
- attributeName: {attributeName}
- value: {value}";
        }

        [McpServerPrompt(Name = "SetBlockAttribute"), Description("Set one attribute of a block, e.g. an OB cycle time")]
        public static string SetBlockAttribute(string softwarePath, string blockPath, string attributeName, string value)
        {
            return $@"Set one writable attribute of a block, e.g. 'CyclicTime' (microseconds) of a cyclic interrupt OB (requires the server started with '--allow-write').

Common parameter values:
- softwarePath: defines the path in the project structure to the PLC software
- blockPath: root-relative path of the block
- attributeName: attribute name as listed by 'GetBlockInfo' (case-sensitive)
- value: new value as text

Use the SetBlockAttribute tool with these parameters:
- softwarePath: {softwarePath}
- blockPath: {blockPath}
- attributeName: {attributeName}
- value: {value}";
        }

        [McpServerPrompt(Name = "CreateOB"), Description("Create an empty organization block of a given type")]
        public static string CreateOB(string softwarePath, string name, string number, string secondaryType, string groupPath = "")
        {
            return $@"Create an empty SCL organization block of a given type and number (requires the server started with '--allow-write'). Fill its body afterwards with 'ImportExternalSource', which keeps type and number.

Common parameter values:
- softwarePath: defines the path in the project structure to the PLC software
- name: name of the new OB, without a slash
- number: OB number, e.g. 30, 40 or 100
- secondaryType: ProgramCycle, CyclicInterrupt, HardwareInterrupt or Startup
- groupPath: root-relative block group; empty uses the Program blocks root

Use the CreateOB tool with these parameters:
- softwarePath: {softwarePath}
- name: {name}
- number: {number}
- secondaryType: {secondaryType}
- groupPath: {groupPath}";
        }

        #endregion
    }
}

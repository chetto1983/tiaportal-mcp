using ModelContextProtocol.Server;
using System.ComponentModel;

namespace TiaMcpServer.ModelContextProtocol
{
    public static partial class McpServer
    {
        // Attribute writes on hardware and blocks.
        //
        // Callers: registered through Program.BuildTools() with '--allow-write' and invoked directly
        // by the tests. Data: ResponseMessage (the value read back is in the message). The changes
        // are in memory until SaveProject.

        [WriteTool]
        [McpServerTool(Name = "SetDeviceItemAttribute", Title = "Set device item attribute", Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true),
         Description("Set one writable hardware parameter of a device item, e.g. 'ActivateHighSpeedCounter' of an HSC. The text value is converted to the attribute's type; some attributes appear only after another one is set, so re-read with 'GetDeviceItemInfo'")]
        public static ResponseMessage SetDeviceItemAttribute(
            [Description("deviceItemPath: defines the path in the project structure to the device item")] string deviceItemPath,
            [Description("attributeName: attribute name as listed by 'GetDeviceItemInfo' (case-sensitive)")] string attributeName,
            [Description("value: new value as text; enums take a name or a number, booleans true/false")] string value)
        {
            return Guarded(nameof(SetDeviceItemAttribute), () =>
            {
                var readBack = Portal.SetDeviceItemAttribute(deviceItemPath, attributeName, value);
                return new ResponseMessage { Message = $"'{deviceItemPath}': {attributeName} = {readBack}. {SaveHint}", Meta = OkMeta() };
            });
        }

        [WriteTool]
        [McpServerTool(Name = "SetBlockAttribute", Title = "Set block attribute", Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true),
         Description("Set one writable attribute of a block, e.g. 'CyclicTime' (microseconds) of a cyclic interrupt OB or 'Number'. 'GetBlockInfo' lists the attributes and their access")]
        public static ResponseMessage SetBlockAttribute(
            [Description("softwarePath: defines the path in the project structure to the PLC software")] string softwarePath,
            [Description("blockPath: root-relative path of the block, e.g. 'Group/Block'")] string blockPath,
            [Description("attributeName: attribute name as listed by 'GetBlockInfo' (case-sensitive)")] string attributeName,
            [Description("value: new value as text; enums take a name or a number, booleans true/false")] string value)
        {
            return Guarded(nameof(SetBlockAttribute), () =>
            {
                var readBack = Portal.SetBlockAttribute(softwarePath, blockPath, attributeName, value);
                return new ResponseMessage { Message = $"'{blockPath}': {attributeName} = {readBack}. {SaveHint}", Meta = OkMeta() };
            });
        }
    }
}

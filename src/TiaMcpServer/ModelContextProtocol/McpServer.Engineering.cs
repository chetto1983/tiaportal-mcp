using ModelContextProtocol.Server;
using System.ComponentModel;

namespace TiaMcpServer.ModelContextProtocol
{
    public static partial class McpServer
    {
        // Attribute writes on hardware and blocks, and typed OB creation.
        //
        // Callers: registered through Program.BuildTools() with '--allow-write' and invoked directly
        // by the tests. Data: ResponseMessage (the value read back is in the message) and
        // ResponseCreated. The changes are in memory until SaveProject.

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

        [WriteTool]
        [McpServerTool(Name = "CreateOB", Title = "Create organization block", Destructive = true, OpenWorld = false, UseStructuredContent = true),
         Description("Create an empty SCL organization block of a given type and number, e.g. a cyclic interrupt OB30. An OB generated from a source always becomes a program cycle OB; fill the body afterwards with 'ImportExternalSource', which keeps type and number")]
        public static ResponseCreated CreateOB(
            [Description("softwarePath: defines the path in the project structure to the PLC software")] string softwarePath,
            [Description("name: name of the new OB, without a slash")] string name,
            [Description("number: OB number, e.g. 30 for a cyclic interrupt, 40 for a hardware interrupt, 100 for startup")] int number,
            [Description("secondaryType: OB type, e.g. ProgramCycle, CyclicInterrupt, HardwareInterrupt or Startup")] string secondaryType,
            [Description("groupPath: root-relative block group that receives the OB; empty uses the Program blocks root")] string groupPath = "")
        {
            return Guarded(nameof(CreateOB), () =>
            {
                var block = Portal.CreateOB(softwarePath, groupPath, name, number, secondaryType);
                return Created("OB", block.Name, JoinPath(groupPath, block.Name));
            });
        }
    }
}

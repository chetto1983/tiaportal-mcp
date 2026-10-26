using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Utilities;
using System.IO;

namespace TiaMcpServer.Siemens
{
    public partial class Portal
    {
        // OPC UA XML export of one PLC.
        //
        // Callers: the ExportOpcUaXml tool in McpServer.OpcUa.cs. Affected API:
        // OpcUaExportProvider.Export(DeviceItem, FileInfo), found through Project.HwUtilities.
        // Writes one .xml file below the path the caller supplies; it does not modify the project,
        // so it is not gated behind '--allow-write'. TIA Portal requires the PLC to be offline
        // ('GoOffline').

        private const string OpcUaExportProviderId = "OPCUAExportProvider";
        private const string OpcUaExportFolder = "OPC UA";

        /// <summary>
        /// Exports the OPC UA data of one PLC to '&lt;exportPath&gt;/&lt;softwarePath&gt;/OPC UA/&lt;PLC name&gt;.xml'.
        /// Returns the PLC's device item and the file that was written.
        /// </summary>
        public (DeviceItem Plc, string FilePath) ExportOpcUaXml(string softwarePath, string exportPath)
        {
            return Operation.Run(_logger, nameof(ExportOpcUaXml), PortalErrorCode.ExportFailed,
                () =>
                {
                    var plc = GetPlcDeviceItemOrThrow(softwarePath);

                    var provider = _project!.HwUtilities.Find(OpcUaExportProviderId) as OpcUaExportProvider
                        ?? throw new PortalException(PortalErrorCode.NotSupported,
                            "OPC UA export is not available in this TIA Portal installation or project.");

                    var directory = Path.Combine(ResolveSoftwareRoot(exportPath, softwarePath), OpcUaExportFolder);
                    var target = Path.Combine(directory, $"{FileNames.Safe(plc.Name)}.xml");

                    Directory.CreateDirectory(directory);

                    if (File.Exists(target))
                    {
                        File.Delete(target);
                    }

                    provider.Export(plc, new FileInfo(target));

                    return (plc, target);
                },
                ("softwarePath", softwarePath), ("exportPath", exportPath));
        }

        // The software container and the software are children of the PLC's device item in the
        // engineering object model; walk up until one is found.
        private static DeviceItem? FindDeviceItem(params object?[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var current = (candidate as IEngineeringObject)?.Parent;
                while (current != null)
                {
                    if (current is DeviceItem deviceItem)
                    {
                        return deviceItem;
                    }

                    current = (current as IEngineeringObject)?.Parent;
                }
            }

            return null;
        }
    }
}

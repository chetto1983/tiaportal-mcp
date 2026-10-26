using Siemens.Engineering;
using System;
using System.Linq;

namespace TiaMcpServer.Siemens
{
    public partial class Portal
    {
        // Writing single attributes of hardware device items and of blocks (TIA Portal Openness V21
        // manual: "Setting attributes of Blocks, DBs & UDTs"; device items expose their parameters
        // as attributes, see the HW parameter description in PublicAPI).
        //
        // Callers: the SetDeviceItemAttribute and SetBlockAttribute tools. Affected API:
        // IEngineeringObject.GetAttributeInfos/GetAttribute/SetAttribute. Reads/writes no data
        // files; the change is in memory until SaveProject.
        //
        // Hardware attributes are dynamic: some appear only after another one is set (on an
        // S7-1200 G2 HSC, the pulse width and cycle time appear only once the compare output is
        // active), so the caller re-reads the item with GetDeviceItemInfo after a change.

        /// <summary>Sets one attribute of a device item and returns the value read back.</summary>
        public object? SetDeviceItemAttribute(string deviceItemPath, string attributeName, string value)
        {
            return Operation.Run(_logger, nameof(SetDeviceItemAttribute), PortalErrorCode.InvalidState,
                () =>
                {
                    var deviceItem = GetDeviceItem(deviceItemPath)
                        ?? throw new PortalException(PortalErrorCode.NotFound,
                            $"Device item not found at '{deviceItemPath}'. Use 'GetProjectTree' to list the device items.");

                    return SetTypedAttribute(deviceItem, attributeName, value, "GetDeviceItemInfo");
                },
                ("deviceItemPath", deviceItemPath), ("attributeName", attributeName), ("value", value));
        }

        /// <summary>Sets one attribute of a block (e.g. an OB's CyclicTime) and returns the value read back.</summary>
        public object? SetBlockAttribute(string softwarePath, string blockPath, string attributeName, string value)
        {
            return Operation.Run(_logger, nameof(SetBlockAttribute), PortalErrorCode.InvalidState,
                () =>
                {
                    var block = GetBlock(softwarePath, blockPath)
                        ?? throw new PortalException(PortalErrorCode.NotFound,
                            $"Block not found at '{blockPath}'. Use 'GetBlocks' to list the available blocks.");

                    EnsureNotKnowHowProtected(block);
                    return SetTypedAttribute(block, attributeName, value, "GetBlockInfo");
                },
                ("softwarePath", softwarePath), ("blockPath", blockPath), ("attributeName", attributeName), ("value", value));
        }

        private static object? SetTypedAttribute(IEngineeringObject target, string attributeName, string value, string listingTool)
        {
            var info = target.GetAttributeInfos().FirstOrDefault(i => i.Name == attributeName)
                ?? throw new PortalException(PortalErrorCode.NotFound,
                    $"Attribute '{attributeName}' not found (names are case-sensitive). '{listingTool}' lists the attributes.");

            if (!info.AccessMode.HasFlag(EngineeringAttributeAccessMode.Write))
            {
                throw new PortalException(PortalErrorCode.NotSupported,
                    $"Attribute '{attributeName}' is read-only ({info.AccessMode}).");
            }

            var current = target.GetAttribute(attributeName);
            object typed;
            try
            {
                typed = current == null ? value : AttributeValues.Convert(value, current.GetType());
            }
            catch (FormatException ex)
            {
                throw new PortalException(PortalErrorCode.InvalidParams,
                    $"Attribute '{attributeName}': {ex.Message}", null, ex);
            }

            target.SetAttribute(attributeName, typed);
            return target.GetAttribute(attributeName);
        }
    }
}

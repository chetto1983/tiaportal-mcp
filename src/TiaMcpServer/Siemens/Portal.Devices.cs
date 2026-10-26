using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.HmiUnified;
using Siemens.Engineering.Multiuser;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Types;
using Siemens.Engineering.Safety;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace TiaMcpServer.Siemens
{
    public partial class Portal
    {
        #region devices

        public List<Device> GetDevices(string regexName = "")
        {
            _logger?.LogInformation("Getting devices...");

            if (IsProjectNull())
            {
                return [];
            }

            return EnumerateAllDevices()
                .Where(device => MatchesRegex(device, regexName))
                .ToList();
        }

        public Device? GetDevice(string devicePath)
        {
            _logger?.LogInformation($"Getting device by path: {devicePath}");

            if (IsProjectNull())
            {
                return null;
            }

            // Retrieve the device by its path
            return GetDeviceByPath(devicePath);
        }

        public DeviceItem? GetDeviceItem(string deviceItemPath)
        {
            _logger?.LogInformation($"Getting device item by path: {deviceItemPath}");

            if (IsProjectNull())
            {
                return null;
            }

            // Retrieve the device by its path
            return GetDeviceItemByPath(deviceItemPath);

        }

        #endregion

        #region get...by path

        private Device? GetDeviceByPath(string devicePath)
        {
            if (_project?.Devices == null || string.IsNullOrWhiteSpace(devicePath))
                return null;

            // A device name can itself contain '/' (e.g. 'S7-1500/ET200MP station_1'), so try the
            // whole input as a name before splitting it into group/device segments.
            var rootDevices = _project.Devices.Concat(EnumerateUngroupedDevices()).ToList();
            var exactMatch = FindDeviceByName(rootDevices, devicePath.Trim('/'));
            if (exactMatch != null)
            {
                return exactMatch;
            }

            var pathSegments = devicePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length == 0)
            {
                return null;
            }

            // Try top-level device first
            if (pathSegments.Length == 1)
            {
                return null;
            }

            // Traverse device groups
            DeviceUserGroupComposition? groups = _project.DeviceGroups;
            DeviceUserGroup? group = groups?.FirstOrDefault(g => g.Name.Equals(pathSegments[0], StringComparison.OrdinalIgnoreCase));

            if (group is null)
            {
                return null;
            }

            for (int i = 1; i < pathSegments.Length; i++)
            {
                // Try to find device in current group; the remaining segments may form one name containing '/'
                var device = FindDeviceByName(group.Devices, string.Join("/", pathSegments.Skip(i)))
                    ?? FindDeviceByName(group.Devices, pathSegments[i]);
                if (device != null)
                {
                    return device;
                }

                // Try to find subgroup
                group = group.Groups.FirstOrDefault(g => g.Name.Equals(pathSegments[i], StringComparison.OrdinalIgnoreCase));
                if (group is null)
                {
                    break;
                }
            }

            return null;
        }

        private static Device? FindDeviceByName(IEnumerable<Device> devices, string name)
        {
            return devices.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private DeviceItem? GetDeviceItemByPath(string deviceItemPath)
        {
            if (_project is null || _project.Devices == null || string.IsNullOrWhiteSpace(deviceItemPath))
            {
                return null;
            }

            // A device name can itself contain '/', so check for such a device before splitting the path
            var deviceItem = GetDeviceItemBySlashDeviceName(deviceItemPath.Trim('/'));
            if (deviceItem != null)
            {
                return deviceItem;
            }

            // Split the device path by '/' to get each device name
            var pathSegments = deviceItemPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            deviceItem = GetDeviceItemBySegments(pathSegments);
            if (deviceItem != null)
            {
                return deviceItem;
            }

            // devices below the "Ungrouped devices" system group are not part of devices or groups
            var ungroupedDevices = _project.UngroupedDevicesGroup?.Devices;
            if (ungroupedDevices != null)
            {
                for (int index = 0; index < pathSegments.Length && deviceItem is null; index++)
                {
                    deviceItem = GetDeviceItemFromDevice(pathSegments, ungroupedDevices, index);
                }
            }

            return deviceItem;
        }

        /// <summary>
        /// Handles paths like 'S7-1500/ET200MP station_1/PLC_1' or 'Group1/S7-1500/ET200MP station_1/PLC_1'
        /// where the device name itself contains '/': the device name is looked up as a whole
        /// (at the start of the path or after a '/') and the rest of the path names the device item,
        /// followed by any nested items below it.
        /// </summary>
        private DeviceItem? GetDeviceItemBySlashDeviceName(string path)
        {
            foreach (var device in EnumerateAllDevices().Where(d => d.Name.IndexOf('/') >= 0))
            {
                var prefix = device.Name + "/";
                var start = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
                if (start < 0 || (start > 0 && path[start - 1] != '/'))
                {
                    continue;
                }

                var itemSegments = path.Substring(start + prefix.Length).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (itemSegments.Length == 0)
                {
                    continue;
                }

                var deviceItem = GetNestedDeviceItem(FindDeviceItemByName(device.DeviceItems, itemSegments[0]), itemSegments, 1);
                if (deviceItem != null)
                {
                    return deviceItem;
                }
            }

            return null;
        }

        private DeviceItem? GetDeviceItemBySegments(string[] pathSegments)
        {
            if (_project is null || _project.Devices == null)
            {
                return null;
            }

            DeviceItem? deviceItem = null;

            // initial devices and groups
            var devices = _project.Devices;
            var groups = _project.DeviceGroups;

            for (int index = 0; index < pathSegments.Length; index++)
            {
                deviceItem = GetDeviceItemFromDevice(pathSegments, devices, index);

                if (deviceItem is null)
                {
                    // search in groups
                    var group = groups?.FirstOrDefault(g => g.Name.Equals(pathSegments[index], StringComparison.OrdinalIgnoreCase));
                    if (group is not null)
                    {
                        devices = group.Devices;
                        if (devices != null)
                        {
                            deviceItem = GetDeviceItemFromDevice(pathSegments, devices, index + 1);
                        }

                        if (deviceItem is not null)
                        {
                            return deviceItem;
                        }

                        // not found, but on the path
                        groups = group.Groups;
                        devices = group.Devices;
                    }
                }
                else if (deviceItem is not null)
                {
                    return deviceItem;
                }
            }

            return deviceItem;
        }

        private static DeviceItem? GetDeviceItemFromDevice(string[] pathSegments, DeviceComposition? devices, int index)
        {
            string segment = pathSegments[index];
            string nextSegment = index + 1 < pathSegments.Length ? pathSegments[index + 1] : string.Empty;

            // a pc based plc has a Device.Name = 'PC-System_1' or something like that, which is visible in the TIA-Portal IDE
            // use segment to find device
            var device = devices.FirstOrDefault(d => d.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
            if (device != null)
            {
                // then use next segment to find device item
                var deviceItem = FindDeviceItemByName(device.DeviceItems, nextSegment);
                return GetNestedDeviceItem(deviceItem, pathSegments, index + 2);
            }

            // a hardware plc has a Device.Name = 'S7-1500/ET200MP-Station_1' or something like that, which is not visible in the TIA-Portal IDE
            var topLevelItem = devices
                .SelectMany(d => d.DeviceItems)
                .FirstOrDefault(di => di.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
            return GetNestedDeviceItem(topLevelItem, pathSegments, index + 1);
        }

        /// <summary>
        /// Follows the remaining path segments down the DeviceItems of each level, so that items below
        /// a CPU (e.g. 'S7-1200 G2 station_1/G2_PLC_1/HSC_1') resolve to the item named by the last
        /// segment. Returns null when any segment does not match, instead of the parent.
        /// </summary>
        private static DeviceItem? GetNestedDeviceItem(DeviceItem? deviceItem, string[] pathSegments, int start)
        {
            for (int index = start; deviceItem != null && index < pathSegments.Length; index++)
            {
                deviceItem = FindDeviceItemByName(deviceItem.DeviceItems, pathSegments[index]);
            }

            return deviceItem;
        }

        private static DeviceItem? FindDeviceItemByName(DeviceItemComposition deviceItems, string name)
        {
            return deviceItems.FirstOrDefault(di => di.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region get recursive ...

        /// <summary>
        /// All devices of the open project: top level, every user group (recursively) and the
        /// "Ungrouped devices" system group, which is a DeviceSystemGroup and not part of DeviceGroups.
        /// </summary>
        private IEnumerable<Device> EnumerateAllDevices()
        {
            if (_project is null)
            {
                yield break;
            }

            if (_project.Devices != null)
            {
                foreach (var device in _project.Devices)
                {
                    yield return device;
                }
            }

            if (_project.DeviceGroups != null)
            {
                foreach (var group in _project.DeviceGroups)
                {
                    foreach (var device in EnumerateGroupDevices(group))
                    {
                        yield return device;
                    }
                }
            }

            foreach (var device in EnumerateUngroupedDevices())
            {
                yield return device;
            }
        }

        private IEnumerable<Device> EnumerateUngroupedDevices()
        {
            var devices = _project?.UngroupedDevicesGroup?.Devices;
            if (devices is null)
            {
                return [];
            }

            return devices;
        }

        private static IEnumerable<Device> EnumerateGroupDevices(DeviceUserGroup group)
        {
            foreach (var device in group.Devices)
            {
                yield return device;
            }

            foreach (var subgroup in group.Groups)
            {
                foreach (var device in EnumerateGroupDevices(subgroup))
                {
                    yield return device;
                }
            }
        }

        private static bool MatchesRegex(Device device, string regexName)
        {
            if (string.IsNullOrEmpty(regexName))
            {
                return true;
            }

            try
            {
                return Regex.IsMatch(device.Name, regexName, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, skip this device
                return false;
            }
        }

        #endregion
    }
}

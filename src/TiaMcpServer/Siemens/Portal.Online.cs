using Siemens.Engineering;
using Siemens.Engineering.Connection;
using Siemens.Engineering.HW;
using Siemens.Engineering.Online;
using System.Collections.Generic;
using System.Linq;

namespace TiaMcpServer.Siemens
{
    public partial class Portal
    {
        // Online connection of a PLC (TIA Portal Openness V21 manual: "Setting PLC Online",
        // "Accessing parameters of an online connection", "Establishing or disconnecting the online
        // connection to the PLC").
        //
        // Callers: the GetOnlineState, GoOnline and GoOffline tools in McpServer.Online.cs, and
        // DownloadToDevice. Affected API: OnlineProvider and ConnectionConfiguration of the PLC's
        // device item. Reads/writes no data files and does not modify the project; it changes only
        // whether TIA Portal is connected to the device. Openness refuses to edit a project while a
        // PLC is online ("This function is not supported in online mode"), so GoOffline is the
        // remedy for that error.

        /// <summary>One way to reach the PLC: connection mode, PG/PC interface and target interface.</summary>
        public sealed class ConnectionTarget
        {
            public string Mode { get; set; } = string.Empty;
            public string PcInterface { get; set; } = string.Empty;
            public int PcInterfaceNumber { get; set; }
            public string TargetInterface { get; set; } = string.Empty;
        }

        public (string State, bool IsConfigured, List<ConnectionTarget> Targets) GetOnlineState(string softwarePath)
        {
            return Operation.Run(_logger, nameof(GetOnlineState), PortalErrorCode.NotSupported,
                () =>
                {
                    var provider = GetOnlineProviderOrThrow(softwarePath);
                    return (provider.State.ToString(), provider.Configuration.IsConfigured, ListTargets(provider.Configuration));
                },
                ("softwarePath", softwarePath));
        }

        /// <summary>
        /// Goes online. With <paramref name="mode"/> the connection is first set to that mode, PG/PC
        /// interface and target interface; without it the connection already configured in TIA
        /// Portal is used. GoOnline can take 1 to 16 seconds (manual).
        /// </summary>
        public string GoOnline(string softwarePath, string mode, string pcInterface, int pcInterfaceNumber, string targetInterface)
        {
            return Operation.Run(_logger, nameof(GoOnline), PortalErrorCode.InvalidState,
                () =>
                {
                    var provider = GetOnlineProviderOrThrow(softwarePath);
                    var configuration = provider.Configuration;

                    if (!string.IsNullOrWhiteSpace(mode))
                    {
                        configuration.ApplyConfiguration(FindTargetInterface(configuration, mode, pcInterface, pcInterfaceNumber, targetInterface));
                    }
                    else if (!configuration.IsConfigured)
                    {
                        throw new PortalException(PortalErrorCode.InvalidParams,
                            "No online connection is configured for this PLC. Pass mode, pcInterface and targetInterface; " +
                            "'GetOnlineState' lists the available ones.");
                    }

                    return provider.GoOnline().ToString();
                },
                ("softwarePath", softwarePath), ("mode", mode), ("pcInterface", pcInterface), ("targetInterface", targetInterface));
        }

        public string GoOffline(string softwarePath)
        {
            return Operation.Run(_logger, nameof(GoOffline), PortalErrorCode.InvalidState,
                () =>
                {
                    var provider = GetOnlineProviderOrThrow(softwarePath);
                    provider.GoOffline();
                    return provider.State.ToString();
                },
                ("softwarePath", softwarePath));
        }

        /// <summary>The device item of the PLC that owns the software at <paramref name="softwarePath"/>.</summary>
        private DeviceItem GetPlcDeviceItemOrThrow(string softwarePath)
        {
            var plcSoftware = GetPlcSoftwareOrThrow(softwarePath);

            return FindDeviceItem(GetSoftwareContainer(softwarePath), plcSoftware)
                ?? throw new PortalException(PortalErrorCode.NotFound,
                    $"No PLC device item found for '{softwarePath}'. Use 'GetSoftwareTree' to list the PLC software.");
        }

        private OnlineProvider GetOnlineProviderOrThrow(string softwarePath)
        {
            return GetPlcDeviceItemOrThrow(softwarePath).GetService<OnlineProvider>()
                ?? throw new PortalException(PortalErrorCode.NotSupported,
                    $"The device of '{softwarePath}' offers no online connection.");
        }

        private static List<ConnectionTarget> ListTargets(ConnectionConfiguration configuration)
        {
            var targets = new List<ConnectionTarget>();

            foreach (ConfigurationMode mode in configuration.Modes)
            {
                foreach (ConfigurationPcInterface pcInterface in mode.PcInterfaces)
                {
                    foreach (ConfigurationTargetInterface target in pcInterface.TargetInterfaces)
                    {
                        targets.Add(new ConnectionTarget
                        {
                            Mode = mode.Name,
                            PcInterface = pcInterface.Name,
                            PcInterfaceNumber = pcInterface.Number,
                            TargetInterface = target.Name
                        });
                    }
                }
            }

            return targets;
        }

        /// <summary>
        /// The target interface named by mode, PG/PC interface (name and number) and target interface
        /// name; an empty target name picks the first one. Throws with the valid choices otherwise.
        /// </summary>
        private static ConfigurationTargetInterface FindTargetInterface(
            ConnectionConfiguration configuration, string mode, string pcInterface, int pcInterfaceNumber, string targetInterface)
        {
            var target = configuration.Modes.Find(mode)?
                .PcInterfaces.Find(pcInterface, pcInterfaceNumber)?
                .TargetInterfaces
                .FirstOrDefault(t => string.IsNullOrEmpty(targetInterface) || t.Name == targetInterface);

            if (target == null)
            {
                var valid = ListTargets(configuration)
                    .Select(t => $"'{t.Mode}' / '{t.PcInterface}' #{t.PcInterfaceNumber} / '{t.TargetInterface}'");
                throw new PortalException(PortalErrorCode.NotFound,
                    $"No connection '{mode}' / '{pcInterface}' #{pcInterfaceNumber} / '{targetInterface}'. Available: " +
                    string.Join("; ", valid));
            }

            return target;
        }
    }
}

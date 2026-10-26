using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Linq;

namespace TiaMcpServer.ModelContextProtocol
{
    public static partial class McpServer
    {
        // Online connection and download of a PLC.
        //
        // Callers: registered through Program.BuildTools() and invoked directly by the tests.
        // Data: ResponseOnlineState, ResponseMessage and ResponseDownload as structuredContent.
        // GetOnlineState, GoOnline and GoOffline do not modify the project, so they are not
        // [WriteTool]s; DownloadToDevice changes the device and is one. None of them runs inside a
        // TIA Portal transaction, because Openness refuses to go online while one is active.

        [McpServerTool(Name = "GetOnlineState", Title = "Get PLC online state", ReadOnly = true, OpenWorld = true, UseStructuredContent = true),
         Description("Show whether TIA Portal is online with a PLC and list the connections (mode, PG/PC interface, target interface) that GoOnline and DownloadToDevice accept")]
        public static ResponseOnlineState GetOnlineState(
            [Description("softwarePath: defines the path in the project structure to the PLC software")] string softwarePath)
        {
            return Unguarded(nameof(GetOnlineState), () =>
            {
                var (state, isConfigured, targets) = Portal.GetOnlineState(softwarePath);
                return new ResponseOnlineState
                {
                    Message = $"PLC of '{softwarePath}' is {state}; {targets.Count} connection(s) available",
                    State = state,
                    IsConfigured = isConfigured,
                    Targets = targets.Select(t => new ResponseConnectionTarget
                    {
                        Mode = t.Mode,
                        PcInterface = t.PcInterface,
                        PcInterfaceNumber = t.PcInterfaceNumber,
                        TargetInterface = t.TargetInterface
                    }).ToList(),
                    Meta = Ok()
                };
            });
        }

        [McpServerTool(Name = "GoOnline", Title = "Go online with PLC", Destructive = false, Idempotent = true, OpenWorld = true, UseStructuredContent = true),
         Description("Connect TIA Portal online to the PLC, through the given connection or the one already configured. While online, Openness refuses to edit the project: call 'GoOffline' before writing")]
        public static ResponseMessage GoOnline(
            [Description("softwarePath: defines the path in the project structure to the PLC software")] string softwarePath,
            [Description("mode: connection mode from 'GetOnlineState', e.g. 'PN/IE'; empty uses the configured connection")] string mode = "",
            [Description("pcInterface: PG/PC interface name from 'GetOnlineState'")] string pcInterface = "",
            [Description("pcInterfaceNumber: PG/PC interface number from 'GetOnlineState' (default 1)")] int pcInterfaceNumber = 1,
            [Description("targetInterface: target interface name from 'GetOnlineState'; empty picks the first")] string targetInterface = "")
        {
            return Unguarded(nameof(GoOnline), () =>
            {
                var state = Portal.GoOnline(softwarePath, mode, pcInterface, pcInterfaceNumber, targetInterface);
                return new ResponseMessage { Message = $"PLC of '{softwarePath}' is {state}", Meta = Ok() };
            });
        }

        [McpServerTool(Name = "GoOffline", Title = "Go offline from PLC", Destructive = false, Idempotent = true, OpenWorld = true, UseStructuredContent = true),
         Description("Disconnect TIA Portal from the PLC. Needed before creating or importing objects when TIA Portal reports 'not supported in online mode'")]
        public static ResponseMessage GoOffline(
            [Description("softwarePath: defines the path in the project structure to the PLC software")] string softwarePath)
        {
            return Unguarded(nameof(GoOffline), () =>
            {
                var state = Portal.GoOffline(softwarePath);
                return new ResponseMessage { Message = $"PLC of '{softwarePath}' is {state}", Meta = Ok() };
            });
        }

        [WriteTool]
        [McpServerTool(Name = "DownloadToDevice", Title = "Download to device", Destructive = true, OpenWorld = true, UseStructuredContent = true),
         Description("Download hardware and/or software to a PLC or a PLCSIM instance and report every load-preview question with the answer given. Never resets the module; stopping the CPU, reinitializing data and accepting a different configuration need an explicit flag")]
        public static ResponseDownload DownloadToDevice(
            [Description("softwarePath: defines the path in the project structure to the PLC software")] string softwarePath,
            [Description("mode: connection mode from 'GetOnlineState', e.g. 'PN/IE'")] string mode,
            [Description("pcInterface: PG/PC interface name from 'GetOnlineState'")] string pcInterface,
            [Description("pcInterfaceNumber: PG/PC interface number from 'GetOnlineState' (default 1)")] int pcInterfaceNumber = 1,
            [Description("targetInterface: target interface name from 'GetOnlineState'; empty picks the first")] string targetInterface = "",
            [Description("options: HardwareAndSoftware (default), Software, SoftwareOnlyChanges or Hardware")] string options = "HardwareAndSoftware",
            [Description("allowStop: allow stopping the CPU for the download and restarting it afterwards (default false)")] bool allowStop = false,
            [Description("reinitialize: allow reinitializing data blocks and memory, losing actual values (default false)")] bool reinitialize = false,
            [Description("acceptDifferentConfiguration: accept a target whose modules differ from the project (default false)")] bool acceptDifferentConfiguration = false)
        {
            return Guarded(nameof(DownloadToDevice), () =>
            {
                var outcome = Portal.DownloadToDevice(softwarePath, mode, pcInterface, pcInterfaceNumber, targetInterface,
                    options, allowStop, reinitialize, acceptDifferentConfiguration);
                var meta = Ok();
                meta["success"] = outcome.State != "Error";
                return new ResponseDownload
                {
                    Message = $"Download to '{softwarePath}' finished with state '{outcome.State}': " +
                              $"{outcome.ErrorCount} error(s), {outcome.WarningCount} warning(s)",
                    State = outcome.State,
                    ErrorCount = outcome.ErrorCount,
                    WarningCount = outcome.WarningCount,
                    Messages = outcome.Messages,
                    Decisions = outcome.Decisions,
                    Meta = meta
                };
            }, useTransaction: false);
        }

        /// <summary>The error handling of the read tools: a PortalException passes its message through.</summary>
        private static T Unguarded<T>(string toolName, Func<T> body)
        {
            try
            {
                return body();
            }
            catch (TiaMcpServer.Siemens.PortalException pex)
            {
                throw new McpException(pex.Message, pex);
            }
            catch (Exception ex) when (ex is not McpException)
            {
                throw new McpException($"Unexpected error in '{toolName}': {ex.Message}", ex);
            }
        }
    }
}

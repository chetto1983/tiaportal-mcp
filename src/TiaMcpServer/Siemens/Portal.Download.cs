using Siemens.Engineering;
using Siemens.Engineering.Download;
using Siemens.Engineering.Download.Configurations;
using System;
using System.Collections.Generic;

namespace TiaMcpServer.Siemens
{
    public partial class Portal
    {
        // Download to a PLC (TIA Portal Openness V21 manual: "Downloading to PLC devices").
        //
        // Callers: the DownloadToDevice tool. Affected API: DownloadProvider.Download with a pre- and
        // a post-download delegate. Each delegate call is a question TIA Portal would ask in the
        // load preview dialog; unanswered questions that block the load abort it. The answers below
        // are conservative: stopping the CPU, reinitializing data and accepting a different target
        // configuration happen only when the caller asks for them, a module reset never, and
        // passwords are not handled. Every question and its answer is returned to the caller.
        // Reads/writes no data files and does not modify the project; it changes the device.

        public sealed class DownloadOutcome
        {
            public string State { get; set; } = string.Empty;
            public int ErrorCount { get; set; }
            public int WarningCount { get; set; }
            public List<string> Messages { get; } = new List<string>();
            public List<string> Decisions { get; } = new List<string>();
        }

        public DownloadOutcome DownloadToDevice(string softwarePath, string mode, string pcInterface, int pcInterfaceNumber,
            string targetInterface, string options, bool allowStop, bool reinitialize, bool acceptDifferentConfiguration)
        {
            return Operation.Run(_logger, nameof(DownloadToDevice), PortalErrorCode.InvalidState,
                () =>
                {
                    var downloadOptions = ParseDownloadOptions(options);
                    var device = GetPlcDeviceItemOrThrow(softwarePath);
                    var provider = device.GetService<DownloadProvider>()
                        ?? throw new PortalException(PortalErrorCode.NotSupported,
                            $"The device of '{softwarePath}' cannot be downloaded to.");
                    var target = FindTargetInterface(provider.Configuration, mode, pcInterface, pcInterfaceNumber, targetInterface);

                    var outcome = new DownloadOutcome();
                    void Answer(DownloadConfiguration configuration) =>
                        outcome.Decisions.Add($"{configuration.GetType().Name}: {Decide(configuration, allowStop, reinitialize, acceptDifferentConfiguration)} ({configuration.Message})");

                    DownloadResult result;
                    try
                    {
                        result = provider.Download(target, Answer, Answer, downloadOptions);
                    }
                    catch (EngineeringException ex) when (!allowStop && ex.Message.Contains(nameof(StopModules)))
                    {
                        throw new PortalException(PortalErrorCode.InvalidState,
                            "This download needs the CPU in STOP. Nothing was loaded and the CPU keeps running; " +
                            "call again with allowStop=true to stop it, load and restart it.", null, ex);
                    }

                    outcome.State = result.State.ToString();
                    outcome.ErrorCount = result.ErrorCount;
                    outcome.WarningCount = result.WarningCount;
                    AddMessages(result.Messages, outcome.Messages, string.Empty);
                    return outcome;
                },
                ("softwarePath", softwarePath), ("mode", mode), ("pcInterface", pcInterface),
                ("targetInterface", targetInterface), ("options", options));
        }

        private static DownloadOptions ParseDownloadOptions(string options)
        {
            switch ((options ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "":
                case "hardwareandsoftware":
                    return DownloadOptions.Hardware | DownloadOptions.Software;
                case "software":
                    return DownloadOptions.Software;
                case "softwareonlychanges":
                    return DownloadOptions.SoftwareOnlyChanges;
                case "hardware":
                    return DownloadOptions.Hardware;
                default:
                    throw new PortalException(PortalErrorCode.InvalidParams,
                        $"Unknown download options '{options}'. Use HardwareAndSoftware, Software, SoftwareOnlyChanges or Hardware.");
            }
        }

        /// <summary>Answers one load-preview question and returns the answer as text.</summary>
        private static string Decide(DownloadConfiguration configuration, bool allowStop, bool reinitialize, bool acceptDifferentConfiguration)
        {
            switch (configuration)
            {
                case StopModules stop:
                    // NoAction is a documented value, but on V21 with a running CPU TIA rejects it with
                    // "The configuration provided is invalid" (measured 2026-10-26 on PLCSIM Advanced):
                    // leave the question open so that TIA aborts the load itself.
                    if (!allowStop)
                    {
                        return "not answered, the load stops here (allowStop is false)";
                    }
                    stop.CurrentSelection = StopModulesSelections.StopAll;
                    return "stop all";
                case StartModules start:
                    start.CurrentSelection = allowStop ? StartModulesSelections.StartModule : StartModulesSelections.NoAction;
                    return allowStop ? "start module" : "not started (allowStop is false)";
                case ConsistentBlocksDownload blocks:
                    blocks.CurrentSelection = ConsistentBlocksDownloadSelections.ConsistentDownload;
                    return "consistent download";
                case AllBlocksDownload allBlocks:
                    allBlocks.CurrentSelection = AllBlocksDownloadSelections.DownloadAllBlocks;
                    return "download all blocks";
                case AlarmTextLibrariesDownload alarms:
                    alarms.CurrentSelection = AlarmTextLibrariesDownloadSelections.ConsistentDownload;
                    return "consistent download";
                case ExpandDownload expand:
                    expand.CurrentSelection = ExpandDownloadSelections.Download;
                    return "download";
                case OverwriteSystemData systemData:
                    systemData.CurrentSelection = OverwriteSystemDataSelections.Overwrite;
                    return "overwrite";
                case DifferentTargetConfiguration different:
                    different.CurrentSelection = acceptDifferentConfiguration
                        ? DifferentTargetConfigurationSelections.AcceptAll : DifferentTargetConfigurationSelections.NoAction;
                    return acceptDifferentConfiguration ? "accept all" : "no action (acceptDifferentConfiguration is false)";
                case InitializeMemory memory:
                    memory.CurrentSelection = reinitialize ? InitializeMemorySelections.AcceptAll : InitializeMemorySelections.NoAction;
                    return reinitialize ? "accept all" : "no action (reinitialize is false)";
                case DataBlockReinitialization reinit:
                    reinit.CurrentSelection = reinitialize
                        ? DataBlockReinitializationSelections.StopPlcAndReinitialize : DataBlockReinitializationSelections.NoAction;
                    return reinitialize ? "stop PLC and reinitialize" : "no action (reinitialize is false)";
                case DataBlockReinitializationOrKeepActualValues keep:
                    keep.CurrentSelection = reinitialize
                        ? DataBlockReinitializationOrKeepActualValuesSelections.StopPlcAndReinitialize
                        : DataBlockReinitializationOrKeepActualValuesSelections.KeepActualValues;
                    return reinitialize ? "stop PLC and reinitialize" : "keep actual values";
                case ResetModule reset:
                    reset.CurrentSelection = ResetModuleSelections.NoAction;
                    return "no action (a module reset is never chosen automatically)";
                case LoadIdentificationData identification:
                    identification.CurrentSelection = LoadIdentificationDataSelections.LoadNothing;
                    return "load nothing";
                case ActiveTestCanBeAborted test:
                    test.CurrentSelection = ActiveTestCanBeAbortedSelections.AcceptAll;
                    return "accept all";
                case ActiveTestCanPreventDownload preventing:
                    preventing.CurrentSelection = ActiveTestCanPreventDownloadSelections.AcceptAll;
                    return "accept all";
                case CheckBeforeDownload check:
                    check.Checked = true;
                    return "checked";
                case DownloadPasswordConfiguration _:
                    return "not answered (passwords are not supported)";
                default:
                    return "not answered";
            }
        }

        private static void AddMessages(DownloadResultMessageComposition? messages, List<string> into, string indent)
        {
            if (messages == null)
            {
                return;
            }

            foreach (var message in messages)
            {
                into.Add($"{indent}{message.State}: {message.Message}");
                AddMessages(message.Messages, into, indent + "  ");
            }
        }
    }
}

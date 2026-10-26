using Siemens.Engineering;
using Siemens.Engineering.SW.Blocks;
using System.IO;
using System.Security;
using System.Text;

namespace TiaMcpServer.Siemens
{
    public partial class Portal
    {
        // Creating an organization block of a given type. Openness has no CreateOB, and an OB
        // generated from an SCL source always becomes a Program cycle OB with an automatic number
        // (measured on V21: number 123, SecondaryType read-only). An OB of another type is created
        // by importing SimaticML that carries <Number> and <SecondaryType>; without SecondaryType
        // the import fails with "Cannot create the 'SW.Blocks.OB' object" (measured). The body is
        // empty SCL: fill it afterwards from a source (ImportExternalSource keeps type and number).
        //
        // Callers: the CreateOB tool. Writes one scratch XML file in a temporary folder that is
        // deleted afterwards; the new OB is in memory until SaveProject.

        public PlcBlock CreateOB(string softwarePath, string groupPath, string name, int number, string secondaryType)
        {
            return Operation.Run(_logger, nameof(CreateOB), PortalErrorCode.CreateFailed,
                () =>
                {
                    EnsureValidName(name);

                    if (string.IsNullOrWhiteSpace(secondaryType))
                    {
                        throw new PortalException(PortalErrorCode.InvalidParams,
                            "secondaryType is required, e.g. ProgramCycle, CyclicInterrupt, HardwareInterrupt or Startup.");
                    }

                    var group = GetPlcBlockGroupByPath(softwarePath, groupPath)
                        ?? throw new PortalException(PortalErrorCode.NotFound,
                            $"Block group not found at '{groupPath}'.");

                    using (var scope = new SourceScope(_logger))
                    {
                        var file = Path.Combine(scope.Directory, "ob.xml");
                        File.WriteAllText(file, ObXml(name, number, secondaryType), new UTF8Encoding(true));

                        // ImportOptions.None: never replace an existing OB of the same name
                        var imported = group.Blocks.Import(new FileInfo(file), ImportOptions.None);
                        if (imported == null || imported.Count == 0)
                        {
                            throw new PortalException(PortalErrorCode.CreateFailed, $"TIA Portal created no OB '{name}'.");
                        }

                        return imported[0];
                    }
                },
                ("softwarePath", softwarePath), ("groupPath", groupPath), ("name", name), ("number", number), ("secondaryType", secondaryType));
        }

        private static string ObXml(string name, int number, string secondaryType)
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Document>
  <Engineering version=""V{Engineering.TiaMajorVersion}"" />
  <SW.Blocks.OB ID=""0"">
    <AttributeList>
      <MemoryLayout>Optimized</MemoryLayout>
      <Name>{SecurityElement.Escape(name)}</Name>
      <Namespace />
      <Number>{number}</Number>
      <ProgrammingLanguage>SCL</ProgrammingLanguage>
      <SecondaryType>{SecurityElement.Escape(secondaryType)}</SecondaryType>
      <SetENOAutomatically>false</SetENOAutomatically>
    </AttributeList>
    <ObjectList>
      <SW.Blocks.CompileUnit ID=""1"" CompositionName=""CompileUnits"">
        <AttributeList>
          <NetworkSource>
            <StructuredText xmlns=""http://www.siemens.com/automation/Openness/SW/NetworkSource/StructuredText/v4"">
              <NewLine UId=""21"" />
            </StructuredText>
          </NetworkSource>
          <ProgrammingLanguage>SCL</ProgrammingLanguage>
        </AttributeList>
      </SW.Blocks.CompileUnit>
    </ObjectList>
  </SW.Blocks.OB>
</Document>";
        }
    }
}

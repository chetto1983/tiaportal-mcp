using TiaMcpServer.Siemens;

namespace TiaMcpServer.Test
{
    [TestClass]
    public class AssemblyHooks
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            // Load Openness the way Program.Main does: the test classes call Openness.Initialize, but
            // without the fallback resolver Siemens.Engineering.Step7 is not found and every test that
            // reflects over the tool attributes fails with FileNotFoundException.
            Engineering.TiaMajorVersion = Settings.TiaMajorVersion;
            Engineering.RegisterResolver();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
        }
    }
}

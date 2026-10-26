using System;
using TiaMcpServer.Siemens;

namespace TiaMcpServer.Test
{
    /// <summary>
    /// Tests for the conversion of attribute values given as text into the type Openness expects.
    /// These tests do not connect to TIA Portal.
    /// </summary>
    [TestClass]
    public class Test9AttributeValues
    {
        private enum Sample
        {
            Off = 0,
            CyclicInterrupt = 3
        }

        [TestMethod]
        public void Convert_UInt32_KeepsTheExactType()
        {
            // Arrange / Act
            var result = AttributeValues.Convert("1", typeof(uint));

            // Assert: Openness rejects a UInt64 where the attribute is a UInt32
            Assert.IsInstanceOfType(result, typeof(uint));
            Assert.AreEqual(1u, result);
        }

        [TestMethod]
        [DataRow("true", true)]
        [DataRow("FALSE", false)]
        [DataRow("1", true)]
        [DataRow("0", false)]
        public void Convert_Boolean_AcceptsWordsAndDigits(string text, bool expected)
        {
            // Arrange / Act
            var result = AttributeValues.Convert(text, typeof(bool));

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        [DataRow("cyclicinterrupt")]
        [DataRow("3")]
        public void Convert_Enum_AcceptsNameOrNumber(string text)
        {
            // Arrange / Act
            var result = AttributeValues.Convert(text, typeof(Sample));

            // Assert
            Assert.AreEqual(Sample.CyclicInterrupt, result);
        }

        [TestMethod]
        public void Convert_Double_UsesTheInvariantCulture()
        {
            // Arrange / Act
            var result = AttributeValues.Convert("0.25", typeof(double));

            // Assert
            Assert.AreEqual(0.25, result);
        }

        [TestMethod]
        [DataRow("abc", typeof(int))]
        [DataRow("4294967296", typeof(uint))]
        [DataRow("maybe", typeof(bool))]
        [DataRow("Missing", typeof(Sample))]
        public void Convert_InvalidText_ThrowsFormatExceptionNamingTheType(string text, Type type)
        {
            // Arrange / Act
            var ex = Assert.ThrowsException<FormatException>(() => AttributeValues.Convert(text, type));

            // Assert
            StringAssert.Contains(ex.Message, type.Name);
        }
    }
}

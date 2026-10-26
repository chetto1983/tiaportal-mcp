using System;
using System.Globalization;

namespace TiaMcpServer.Siemens
{
    /// <summary>
    /// Converts an attribute value given as text by the caller into the .NET type of the attribute's
    /// current value. Openness rejects a value of another type even when it would fit: setting the
    /// UInt32 hardware attribute 'HscOutputPulseForCountingEventGenerationActive' of an S7-1200 G2
    /// HSC with a UInt64 fails with "The type of the argument ... is invalid" (measured on V21).
    ///
    /// Callers: Portal.SetDeviceItemAttribute and Portal.SetBlockAttribute. Reads/writes no data files.
    /// </summary>
    public static class AttributeValues
    {
        /// <summary>
        /// Returns <paramref name="text"/> converted to <paramref name="targetType"/>. Enums accept a
        /// member name (any case) or its number; booleans accept true/false/1/0; numbers use the
        /// invariant culture. Throws <see cref="FormatException"/> naming the expected type.
        /// </summary>
        public static object Convert(string text, Type targetType)
        {
            var value = (text ?? string.Empty).Trim();

            try
            {
                if (targetType == typeof(string))
                {
                    return value;
                }

                if (targetType == typeof(bool))
                {
                    switch (value.ToLowerInvariant())
                    {
                        case "true":
                        case "1":
                            return true;
                        case "false":
                        case "0":
                            return false;
                    }

                    throw new FormatException();
                }

                if (targetType.IsEnum)
                {
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                    {
                        return Enum.ToObject(targetType, number);
                    }

                    return Enum.Parse(targetType, value, ignoreCase: true);
                }

                return System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException || ex is ArgumentException || ex is InvalidCastException)
            {
                var allowed = targetType.IsEnum ? $" ({string.Join(", ", Enum.GetNames(targetType))})" : string.Empty;
                throw new FormatException($"'{value}' is not a valid {targetType.Name}{allowed}.", ex);
            }
        }
    }
}

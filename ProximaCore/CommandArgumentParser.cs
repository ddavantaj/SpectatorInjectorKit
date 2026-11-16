using System;
using System.Globalization;

namespace M3AR.LivPanel.Core
{
    public static class CommandArgumentParser
    {
        public static bool TryParse(string parameterName, Type type, string text, out object value, out string error)
        {
            string trimmed = text == null ? string.Empty : text.Trim();

            switch (CommandTypeMap.Classify(type))
            {
                case CommandParameterKind.Int:
                    return TryParseInt(parameterName, trimmed, out value, out error);
                case CommandParameterKind.Long:
                    return TryParseLong(parameterName, trimmed, out value, out error);
                case CommandParameterKind.Float:
                    return TryParseFloat(parameterName, trimmed, out value, out error);
                case CommandParameterKind.Bool:
                    return TryParseBool(parameterName, trimmed, out value, out error);
                case CommandParameterKind.Text:
                    value = text ?? string.Empty;
                    error = null;

                    return true;
                case CommandParameterKind.Choice:
                    return TryParseChoice(parameterName, type, trimmed, out value, out error);
                default:
                    value = null;
                    error = CommandTypeMap.DescribeUnsupported(parameterName, CommandTypeMap.FriendlyName(type));

                    return false;
            }
        }

        private static bool TryParseInt(string parameterName, string text, out object value, out string error)
        {
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                value = parsed;
                error = null;

                return true;
            }

            value = null;
            error = "'" + parameterName + "' trebuie sa fie un numar intreg.";

            return false;
        }

        private static bool TryParseLong(string parameterName, string text, out object value, out string error)
        {
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                value = parsed;
                error = null;

                return true;
            }

            value = null;
            error = "'" + parameterName + "' trebuie sa fie un numar intreg.";

            return false;
        }

        private static bool TryParseFloat(string parameterName, string text, out object value, out string error)
        {
            string normalized = text.Replace(',', '.');

            if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                && !float.IsNaN(parsed)
                && !float.IsInfinity(parsed))
            {
                value = parsed;
                error = null;

                return true;
            }

            value = null;
            error = "'" + parameterName + "' trebuie sa fie un numar (ex. 0.5).";

            return false;
        }

        private static bool TryParseBool(string parameterName, string text, out object value, out string error)
        {
            if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1")
            {
                value = true;
                error = null;

                return true;
            }

            if (text.Equals("false", StringComparison.OrdinalIgnoreCase) || text == "0")
            {
                value = false;
                error = null;

                return true;
            }

            value = null;
            error = "'" + parameterName + "' trebuie sa fie true sau false.";

            return false;
        }

        private static bool TryParseChoice(string parameterName, Type type, string text, out object value, out string error)
        {
            string[] names = Enum.GetNames(type);

            foreach (string name in names)
            {
                if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                {
                    value = Enum.Parse(type, name);
                    error = null;

                    return true;
                }
            }

            value = null;
            error = "'" + parameterName + "' trebuie sa fie una dintre: " + string.Join(", ", names) + ".";

            return false;
        }
    }
}

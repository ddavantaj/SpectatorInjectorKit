using System;
using System.Text;

namespace M3AR.LivPanel.Core
{
    public static class CommandTypeMap
    {
        public static CommandParameterKind Classify(Type type)
        {
            if (type == typeof(int))
            {
                return CommandParameterKind.Int;
            }

            if (type == typeof(long))
            {
                return CommandParameterKind.Long;
            }

            if (type == typeof(float))
            {
                return CommandParameterKind.Float;
            }

            if (type == typeof(bool))
            {
                return CommandParameterKind.Bool;
            }

            if (type == typeof(string))
            {
                return CommandParameterKind.Text;
            }

            if (type != null && type.IsEnum)
            {
                return CommandParameterKind.Choice;
            }

            return CommandParameterKind.Unsupported;
        }

        public static string FriendlyName(Type type)
        {
            if (type == null)
            {
                return "?";
            }

            if (!type.IsGenericType)
            {
                return type.Name;
            }

            string name = type.Name;
            int tickIndex = name.IndexOf('`');
            var builder = new StringBuilder(tickIndex >= 0 ? name.Substring(0, tickIndex) : name);
            Type[] arguments = type.GetGenericArguments();

            builder.Append('<');

            for (int index = 0; index < arguments.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(FriendlyName(arguments[index]));
            }

            builder.Append('>');

            return builder.ToString();
        }

        public static string DescribeUnsupported(string parameterName, string typeName)
        {
            return "Parametrul '" + parameterName + "' are tipul " + typeName + ", nesuportat in panou.";
        }
    }
}

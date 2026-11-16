using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace M3AR.LivPanel.Core
{
    public static class CommandDescriptorBuilder
    {
        public static CommandDescriptor FromMethod(
            MethodInfo method,
            string category,
            string alias,
            string description,
            string exampleInput,
            string exampleOutput)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            ParameterInfo[] parameters = method.GetParameters();
            var infos = new List<CommandParameterInfo>(parameters.Length);

            foreach (ParameterInfo parameter in parameters)
            {
                infos.Add(BuildParameter(parameter));
            }

            return new CommandDescriptor(method.Name, alias, category, description, exampleInput, exampleOutput, infos);
        }

        private static CommandParameterInfo BuildParameter(ParameterInfo parameter)
        {
            Type type = parameter.ParameterType;
            CommandParameterKind kind = CommandTypeMap.Classify(type);
            string[] choices = kind == CommandParameterKind.Choice ? Enum.GetNames(type) : null;
            string defaultText = parameter.HasDefaultValue ? DefaultToText(type, kind, parameter.DefaultValue) : string.Empty;

            return new CommandParameterInfo(
                parameter.Name,
                kind,
                CommandTypeMap.FriendlyName(type),
                parameter.HasDefaultValue,
                defaultText,
                choices);
        }

        private static string DefaultToText(Type type, CommandParameterKind kind, object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (kind == CommandParameterKind.Choice)
            {
                return Enum.ToObject(type, value).ToString();
            }

            if (value is bool flag)
            {
                return flag ? "true" : "false";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}

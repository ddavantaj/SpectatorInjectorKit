#if UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using M3AR.LivPanel.Core;
using M3AR.MixedReality.Core.Control;

namespace Common.Utils.MixedReality
{
    /// <summary>Turns the command descriptors of the F1 panel core into the unity-free DTOs of the control protocol.</summary>
    public static class MrCommandInfoFactory
    {
        public static MrCommandInfo FromDescriptor(CommandDescriptor command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var parameters = new MrCommandParameter[command.Parameters.Count];

            for (int index = 0; index < parameters.Length; index++)
            {
                CommandParameterInfo parameter = command.Parameters[index];
                var choices = new List<string>(parameter.Choices);

                parameters[index] = new MrCommandParameter
                {
                    name = parameter.Name,
                    typeName = parameter.TypeName,
                    kind = KindOf(parameter.Kind),
                    isRequired = parameter.IsRequired,
                    defaultText = parameter.DefaultText,
                    choices = choices.ToArray(),
                };
            }

            return new MrCommandInfo
            {
                name = command.Name,
                displayName = command.DisplayName,
                alias = command.Alias,
                category = command.Category,
                description = command.Description,
                exampleInput = command.ExampleInput,
                exampleOutput = command.ExampleOutput,
                isRunnable = command.IsRunnable,
                unsupportedReason = command.UnsupportedReason ?? string.Empty,
                requiredParameterCount = command.RequiredParameterCount,
                parameters = parameters,
            };
        }

        public static string KindOf(CommandParameterKind kind)
        {
            switch (kind)
            {
                case CommandParameterKind.Bool:
                    return MrCommandParameter.KindBool;
                case CommandParameterKind.Choice:
                    return MrCommandParameter.KindChoice;
                case CommandParameterKind.Unsupported:
                    return MrCommandParameter.KindUnsupported;
                default:
                    return MrCommandParameter.KindText;
            }
        }
    }
}
#endif

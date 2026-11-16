using System.Collections.Generic;

namespace M3AR.LivPanel.Core
{
    public sealed class CommandDescriptor
    {
        private static readonly CommandParameterInfo[] NoParameters = new CommandParameterInfo[0];

        public CommandDescriptor(
            string name,
            string alias,
            string category,
            string description,
            string exampleInput,
            string exampleOutput,
            IReadOnlyList<CommandParameterInfo> parameters)
        {
            Name = name ?? string.Empty;
            Alias = alias ?? string.Empty;
            Category = category ?? string.Empty;
            Description = description ?? string.Empty;
            ExampleInput = exampleInput ?? string.Empty;
            ExampleOutput = exampleOutput ?? string.Empty;
            Parameters = parameters ?? NoParameters;
            UnsupportedReason = FindUnsupportedReason(Parameters);
            RequiredParameterCount = CountRequired(Parameters);
        }

        public string Name { get; }

        public string Alias { get; }

        public string Category { get; }

        public string Description { get; }

        public string ExampleInput { get; }

        public string ExampleOutput { get; }

        public IReadOnlyList<CommandParameterInfo> Parameters { get; }

        public string DisplayName => Alias.Length > 0 ? Alias : Name;

        public int RequiredParameterCount { get; }

        public string UnsupportedReason { get; }

        public bool IsRunnable => UnsupportedReason == null;

        private static string FindUnsupportedReason(IReadOnlyList<CommandParameterInfo> parameters)
        {
            foreach (CommandParameterInfo parameter in parameters)
            {
                if (!parameter.IsSupported)
                {
                    return CommandTypeMap.DescribeUnsupported(parameter.Name, parameter.TypeName);
                }
            }

            return null;
        }

        private static int CountRequired(IReadOnlyList<CommandParameterInfo> parameters)
        {
            int count = 0;

            foreach (CommandParameterInfo parameter in parameters)
            {
                if (parameter.IsRequired)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

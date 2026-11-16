using System.Collections.Generic;

namespace M3AR.LivPanel.Core
{
    public sealed class CommandParameterInfo
    {
        private static readonly string[] NoChoices = new string[0];

        public CommandParameterInfo(
            string name,
            CommandParameterKind kind,
            string typeName,
            bool hasDefault,
            string defaultText,
            IReadOnlyList<string> choices)
        {
            Name = name ?? string.Empty;
            Kind = kind;
            TypeName = typeName ?? string.Empty;
            HasDefault = hasDefault;
            DefaultText = defaultText ?? string.Empty;
            Choices = choices ?? NoChoices;
        }

        public string Name { get; }

        public CommandParameterKind Kind { get; }

        public string TypeName { get; }

        public bool HasDefault { get; }

        public string DefaultText { get; }

        public IReadOnlyList<string> Choices { get; }

        public bool IsSupported => Kind != CommandParameterKind.Unsupported;

        public bool IsRequired => !HasDefault;
    }
}

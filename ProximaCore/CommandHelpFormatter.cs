using System.Collections.Generic;
using System.Text;

namespace M3AR.LivPanel.Core
{
    public static class CommandHelpFormatter
    {
        private const string EmptyDefaultText = "(gol)";

        public static string Usage(CommandDescriptor command)
        {
            var builder = new StringBuilder(command.DisplayName);

            foreach (CommandParameterInfo parameter in command.Parameters)
            {
                builder.Append(' ');
                builder.Append(parameter.IsRequired ? parameter.Name : "<" + parameter.Name + ">");
            }

            return builder.ToString();
        }

        public static string Format(CommandDescriptor command)
        {
            var builder = new StringBuilder();

            builder.Append(command.DisplayName);

            if (command.Alias.Length > 0)
            {
                builder.Append(" (").Append(command.Name).Append(')');
            }

            builder.Append('\n');
            builder.Append("Categorie: ").Append(command.Category).Append('\n');

            if (!string.IsNullOrWhiteSpace(command.Description))
            {
                builder.Append("Descriere: ").Append(command.Description).Append('\n');
            }

            builder.Append("Utilizare: ").Append(Usage(command)).Append('\n');

            if (command.Parameters.Count > 0)
            {
                builder.Append("Parametri:\n");

                foreach (CommandParameterInfo parameter in command.Parameters)
                {
                    AppendParameter(builder, parameter);
                }
            }

            if (!string.IsNullOrWhiteSpace(command.ExampleInput))
            {
                builder.Append("Exemplu:\n");
                builder.Append("  > ").Append(command.ExampleInput).Append('\n');

                if (!string.IsNullOrWhiteSpace(command.ExampleOutput))
                {
                    builder.Append("  ").Append(IndentLines(command.ExampleOutput)).Append('\n');
                }
            }

            if (!command.IsRunnable)
            {
                builder.Append("Indisponibil: ").Append(command.UnsupportedReason).Append('\n');
            }

            return builder.ToString().TrimEnd('\n');
        }

        private static void AppendParameter(StringBuilder builder, CommandParameterInfo parameter)
        {
            var details = new List<string>();

            if (!parameter.IsSupported)
            {
                details.Add("nesuportat");
            }

            if (parameter.IsRequired)
            {
                details.Add("obligatoriu");
            }
            else
            {
                details.Add("implicit " + (parameter.DefaultText.Length == 0 ? EmptyDefaultText : parameter.DefaultText));
            }

            if (parameter.Choices.Count > 0)
            {
                details.Add("optiuni: " + string.Join(", ", parameter.Choices));
            }

            builder.Append("  ").Append(parameter.Name).Append(": ").Append(parameter.TypeName);
            builder.Append(" (").Append(string.Join(", ", details)).Append(")\n");
        }

        private static string IndentLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\n", "\n  ");
        }
    }
}

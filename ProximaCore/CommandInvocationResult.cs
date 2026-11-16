namespace M3AR.LivPanel.Core
{
    public sealed class CommandInvocationResult
    {
        private CommandInvocationResult(bool success, string text)
        {
            Success = success;
            Text = text ?? string.Empty;
        }

        public bool Success { get; }

        public string Text { get; }

        public static CommandInvocationResult Succeeded(string text)
        {
            return new CommandInvocationResult(true, text);
        }

        public static CommandInvocationResult Failed(string text)
        {
            return new CommandInvocationResult(false, text);
        }
    }
}

using System;
using System.Collections.Generic;

namespace M3AR.MixedReality.Core.Control
{
    [Serializable] public sealed class MrCommandParameter
    {
        public const string KindText = "text", KindBool = "bool", KindChoice = "choice", KindUnsupported = "unsupported";
        public string name = "", typeName = "", kind = KindText, defaultText = "";
        public bool isRequired;
        public string[] choices = new string[0];
    }
    [Serializable] public sealed class MrCommandInfo
    {
        public string name = "", displayName = "", alias = "", category = "", description = "", exampleInput = "", exampleOutput = "", unsupportedReason = "";
        public bool isRunnable = true;
        public int requiredParameterCount;
        public MrCommandParameter[] parameters = new MrCommandParameter[0];
        public string DisplayNameOrName => !string.IsNullOrEmpty(displayName) ? displayName : !string.IsNullOrEmpty(alias) ? alias : name;
    }
    public interface IMrCommandBackend
    {
        int Revision { get; }
        IReadOnlyList<MrCommandInfo> Commands { get; }
        bool IsBusy { get; }
        string LastError { get; }
        void Refresh();
        string HelpText(string name);
        void Run(string name, string[] args, Action<bool, string> done);
        bool IsRunning(string name);
    }
    public static class MrCommandBackends { public static IMrCommandBackend Current { get; set; } }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Common.Utils.MixedReality;
using M3AR.MixedReality.Core.Control;
using UnityEngine;

namespace SpectatorTools
{
    public partial class SpectatorCameraController
    {
        private ProximaReflectionBackend commandBackend;
        private List<MrCommandInfo> commandCatalog = new List<MrCommandInfo>();
        private int commandRevision = -1;
        private float nextCatalogCheck;
        private bool refreshCommands;
        private string commandSearch = "";
        private string commandCategory = "Toate";
        private string selectedCommandName = "";
        private string[] commandArguments = new string[0];
        private Vector2 categoryScroll, commandsScroll;

        private void UpdateCommandCatalog()
        {
            if (Time.unscaledTime < nextCatalogCheck && !refreshCommands) return;
            nextCatalogCheck = Time.unscaledTime + 1f;
            if (commandBackend == null) commandBackend = ProximaReflectionBackend.TryCreate();
            if (commandBackend == null) return;
            if (refreshCommands) { commandBackend.Refresh(); refreshCommands = false; }
            int revision = commandBackend.Revision;
            if (revision == commandRevision) return;
            commandRevision = revision;
            commandCatalog = commandBackend.Commands.OrderBy(c => c.category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.DisplayNameOrName, StringComparer.OrdinalIgnoreCase).ToList();
            Debug.Log("[Spectator Proxima] Catalog: " + commandCatalog.Count + " comenzi.");
        }

        private void SelectCommand(MrCommandInfo command)
        {
            selectedCommandName = command.name;
            commandArguments = command.parameters.Select(p => p.isRequired ? "" : p.defaultText ?? "").ToArray();
        }

        private void RunCatalogCommand(MrCommandInfo command, string[] arguments)
        {
            if (commandBackend == null || command == null) return;
            string display = command.DisplayNameOrName;
            commandBackend.Run(command.name, arguments, (ok, message) => MainThreadDispatcher.Enqueue(() =>
            {
                terminalLog += "\n[" + (ok ? "OK" : "EROARE") + "] " + display + ": " + message;
                if (terminalLog.Length > 12000) terminalLog = terminalLog.Substring(terminalLog.Length - 12000);
            }));
        }

        private void RunQuick(string name, params string[] args)
        {
            MrCommandInfo command = commandCatalog.FirstOrDefault(c => string.Equals(c.name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.alias, name, StringComparison.OrdinalIgnoreCase));
            if (command == null) { terminalLog += "\n[EROARE] Comanda nu este disponibilă: " + name; return; }
            RunCatalogCommand(command, args);
        }

        private void DrawTabConsole()
        {
            GUILayout.BeginVertical("box");
            DrawSectionTitle("Comenzi Proxima", "Comenzi descoperite din joc; alege categoria și completează parametrii.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Meniu")) RunQuick("load_menu");
            if (GUILayout.Button("Încarcă nivel")) RunQuick("load_level");
            GUILayout.Label("Nivel", GUILayout.Width(38));
            inputLevelStr = GUILayout.TextField(inputLevelStr, GUILayout.Width(50));
            if (GUILayout.Button("Setează", GUILayout.Width(68))) RunQuick("set_level", inputLevelStr);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Caută", GUILayout.Width(45));
            commandSearch = GUILayout.TextField(commandSearch, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("×", GUILayout.Width(28))) commandSearch = "";
            if (GUILayout.Button("Refresh", GUILayout.Width(65))) refreshCommands = true;
            GUILayout.EndHorizontal();

            if (commandBackend == null) { GUILayout.Label("Aștept inițializarea Proxima în joc.", stHint); GUILayout.EndVertical(); return; }
            var categories = commandCatalog.Select(c => string.IsNullOrEmpty(c.category) ? "Altele" : c.category)
                .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
            GUILayout.Label("Categorii", stStatus);
            categoryScroll = GUILayout.BeginScrollView(categoryScroll, GUILayout.Height(70));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(commandCategory == "Toate", "Toate", "Button", GUILayout.Width(75))) commandCategory = "Toate";
            foreach (string category in categories)
                if (GUILayout.Toggle(commandCategory == category, category, "Button", GUILayout.Width(Mathf.Clamp(category.Length * 8 + 24, 75, 160)))) commandCategory = category;
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            string query = commandSearch.Trim();
            var filtered = commandCatalog.Where(c => (commandCategory == "Toate" || string.Equals(string.IsNullOrEmpty(c.category) ? "Altele" : c.category, commandCategory, StringComparison.OrdinalIgnoreCase))
                && (query.Length == 0 || (c.name + " " + c.alias + " " + c.category + " " + c.description).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            GUILayout.Label(filtered.Count + " afișate / " + commandCatalog.Count + " comenzi", stStatus);
            commandsScroll = GUILayout.BeginScrollView(commandsScroll, GUILayout.Height(190));
            foreach (MrCommandInfo command in filtered)
            {
                string label = command.DisplayNameOrName + "   ·   " + (string.IsNullOrEmpty(command.category) ? "Altele" : command.category);
                if (GUILayout.Toggle(selectedCommandName == command.name, label, "Button", GUILayout.Height(24)))
                    if (selectedCommandName != command.name) SelectCommand(command);
            }
            GUILayout.EndScrollView();
            MrCommandInfo selected = commandCatalog.FirstOrDefault(c => c.name == selectedCommandName);
            if (selected != null)
            {
                GUILayout.Space(4);
                GUILayout.Label("<b>" + selected.DisplayNameOrName + "</b>  ·  " + selected.category, stSection);
                if (!string.IsNullOrEmpty(selected.description)) GUILayout.Label(selected.description, stHint);
                for (int i = 0; i < selected.parameters.Length; i++)
                {
                    MrCommandParameter p = selected.parameters[i];
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(p.name + " (" + p.typeName + (p.isRequired ? ", obligatoriu" : ", opțional") + ")", stStatus, GUILayout.Width(180));
                    commandArguments[i] = GUILayout.TextField(commandArguments[i] ?? "", GUILayout.ExpandWidth(true));
                    GUILayout.EndHorizontal();
                    if (p.kind == MrCommandParameter.KindBool) GUILayout.Label("Valori: true / false", stHint);
                    if (p.kind == MrCommandParameter.KindChoice) GUILayout.Label("Valori: " + string.Join(", ", p.choices), stHint);
                }
                if (!string.IsNullOrEmpty(selected.exampleInput)) GUILayout.Label("Exemplu: " + selected.exampleInput, stHint);
                if (!selected.isRunnable) GUILayout.Label(selected.unsupportedReason, stHint);
                GUI.enabled = selected.isRunnable && !commandBackend.IsRunning(selected.name) && !commandBackend.IsBusy;
                if (GUILayout.Button("Rulează comanda", GUILayout.Height(30))) RunCatalogCommand(selected, (string[])commandArguments.Clone());
                GUI.enabled = true;
            }
            GUILayout.EndVertical();
            GUILayout.Space(4);
            GUILayout.BeginVertical("box");
            DrawSectionTitle("Jurnal", null);
            logScrollPos = GUILayout.BeginScrollView(logScrollPos, GUILayout.Height(100));
            GUILayout.TextArea(terminalLog, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            if (GUILayout.Button("Golește jurnalul")) terminalLog = "";
            GUILayout.EndVertical();
        }
    }
}

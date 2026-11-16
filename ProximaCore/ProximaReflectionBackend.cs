#if UNITY_STANDALONE_WIN
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using M3AR.LivPanel.Core;
using M3AR.MixedReality.Core.Control;

namespace Common.Utils.MixedReality
{
    /// <summary>
    /// The Proxima commands of the game for the separate control window, found with reflection only: the module has no
    /// compile-time reference to Proxima, Services.ScenesLoad or Assembly-CSharp, so the same module works in any game build
    /// (also one that was patched afterwards by the installer). Call <see cref="TryRegister"/> once at start; it registers the
    /// backend only when the game carries Proxima (the attribute type exists).
    /// <para>Commands come from <c>Proxima.ProximaInspector.Commands</c> when it exists and is not empty (the same source as the
    /// old in-game "Comenzi" tab), otherwise from a one-off scan of the loaded assemblies for methods that carry
    /// <c>Proxima.ProximaCommandAttribute</c> (repeated on <see cref="Refresh"/>). Category "Internal" is skipped.
    /// Commands run through <see cref="CommandInvoker"/>: static methods only; an instance method is listed and answers
    /// "Comanda 'X' nu e statica." when run, exactly like the in-game tab did.</para>
    /// All members are called on the main thread by the control server.
    /// </summary>
    public sealed class ProximaReflectionBackend : IMrCommandBackend
    {
        public const string AttributeTypeName = "Proxima.ProximaCommandAttribute";
        public const string InspectorTypeName = "Proxima.ProximaInspector";
        public const string SceneLoaderTypeName = "Services.ScenesLoad.SceneLoader";

        private const string InternalCategory = "Internal";
        private const string UnknownCommandText = "Comanda nu mai e inregistrata. Apasa Refresh.";
        private const BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        // Assemblies that cannot hold game commands; skipped by the scan.
        private static readonly string[] SkippedAssemblyPrefixes =
        {
            "mscorlib", "System", "netstandard", "Microsoft.", "Mono.", "UnityEngine", "UnityEditor", "Unity.", "nunit", "Newtonsoft",
            "M3AR.",
        };

        private readonly Type _attributeType;
        private readonly Type _inspectorType;
        private readonly string _sceneLoaderTypeName;
        private readonly MemberInfo _categoryMember;
        private readonly MemberInfo _aliasMember;
        private readonly MemberInfo _descriptionMember;
        private readonly MemberInfo _exampleInputMember;
        private readonly MemberInfo _exampleOutputMember;

        private readonly List<MrCommandInfo> _commands = new List<MrCommandInfo>();
        private readonly Dictionary<string, MethodInfo> _methods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        private readonly Dictionary<string, CommandDescriptor> _descriptors = new Dictionary<string, CommandDescriptor>(StringComparer.Ordinal);
        private readonly HashSet<string> _running = new HashSet<string>(StringComparer.Ordinal);
        private readonly object _runGate = new object();

        private List<MethodInfo> _scanned;
        private PropertyInfo _countProperty;
        private Type _countPropertyOwner;
        private Func<bool> _loadingReader;
        private bool _loadingResolved;
        private int _seenInspectorCount = -1;
        private int _revision;
        private string _lastError = string.Empty;

        private ProximaReflectionBackend(Type attributeType, Type inspectorType, string sceneLoaderTypeName)
        {
            _attributeType = attributeType;
            _inspectorType = inspectorType;
            _sceneLoaderTypeName = sceneLoaderTypeName;
            _categoryMember = FindMember(attributeType, "Category");
            _aliasMember = FindMember(attributeType, "Alias");
            _descriptionMember = FindMember(attributeType, "Description");
            _exampleInputMember = FindMember(attributeType, "ExampleInput");
            _exampleOutputMember = FindMember(attributeType, "ExampleOutput");
        }

        /// <summary>
        /// Sets <see cref="MrCommandBackends.Current"/> to a reflection backend when it is still null and the game has Proxima.
        /// False when something is already registered or the attribute type is not there (it may be called again later).
        /// </summary>
        public static bool TryRegister()
        {
            if (MrCommandBackends.Current != null)
            {
                return false;
            }

            ProximaReflectionBackend backend = TryCreate();

            if (backend == null)
            {
                return false;
            }

            MrCommandBackends.Current = backend;

            return true;
        }

        /// <summary>The backend, or null when <c>Proxima.ProximaCommandAttribute</c> is not in the game.</summary>
        public static ProximaReflectionBackend TryCreate()
        {
            return TryCreate(AttributeTypeName, InspectorTypeName, SceneLoaderTypeName);
        }

        internal static ProximaReflectionBackend TryCreate(string attributeTypeName, string inspectorTypeName, string sceneLoaderTypeName)
        {
            try
            {
                Type attribute = FindType(attributeTypeName, true);

                if (attribute == null || !typeof(Attribute).IsAssignableFrom(attribute))
                {
                    return null;
                }

                return new ProximaReflectionBackend(attribute, FindType(inspectorTypeName, false), sceneLoaderTypeName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public int Revision
        {
            get
            {
                EnsureCurrent();

                return _revision;
            }
        }

        public IReadOnlyList<MrCommandInfo> Commands
        {
            get
            {
                EnsureCurrent();

                return _commands;
            }
        }

        /// <summary>True while <c>Services.ScenesLoad.SceneLoader.IsLoading</c> is true; false when the game has no such member.</summary>
        public bool IsBusy
        {
            get
            {
                if (!_loadingResolved)
                {
                    ResolveLoadingReader();
                }

                try
                {
                    return _loadingReader != null && _loadingReader();
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public string LastError => _lastError;

        public void Refresh()
        {
            _scanned = null;
            Rebuild();
        }

        public string HelpText(string name)
        {
            EnsureCurrent();

            return name != null && _descriptors.TryGetValue(name, out CommandDescriptor descriptor)
                ? CommandHelpFormatter.Format(descriptor)
                : string.Empty;
        }

        public bool IsRunning(string name)
        {
            lock (_runGate)
            {
                return name != null && _running.Contains(name);
            }
        }

        public void Run(string name, string[] args, Action<bool, string> done)
        {
            EnsureCurrent();

            if (name == null || !_methods.TryGetValue(name, out MethodInfo method) || !_descriptors.TryGetValue(name, out CommandDescriptor descriptor))
            {
                done(false, UnknownCommandText);

                return;
            }

            if (!descriptor.IsRunnable)
            {
                done(false, descriptor.UnsupportedReason);

                return;
            }

            if (!CommandInvoker.TryBuildArguments(method, args, out _, out string error))
            {
                done(false, error);

                return;
            }

            lock (_runGate)
            {
                if (!_running.Add(name))
                {
                    done(false, "Comanda ruleaza deja.");

                    return;
                }
            }

            _ = RunAsync(name, method, args, done);
        }

        private async Task RunAsync(string name, MethodInfo method, string[] args, Action<bool, string> done)
        {
            bool success;
            string text;

            try
            {
                CommandInvocationResult result = await CommandInvoker.InvokeAsync(method, args);
                success = result.Success;
                text = result.Text;
            }
            catch (Exception exception)
            {
                success = false;
                text = exception.GetType().Name + ": " + exception.Message;
            }

            lock (_runGate)
            {
                _running.Remove(name);
            }

            done(success, text);
        }

        /// <summary>Commands register late (when the game reaches the menu): a changed count in the inspector rebuilds the list.</summary>
        private void EnsureCurrent()
        {
            int count = InspectorCount();

            if (count != _seenInspectorCount)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            _lastError = string.Empty;
            _commands.Clear();
            _methods.Clear();
            _descriptors.Clear();
            _seenInspectorCount = InspectorCount();
            _revision++;
            ResolveLoadingReader();

            var seen = new HashSet<MethodInfo>();
            int failedCount = 0;
            string lastFailure = string.Empty;

            foreach (MethodInfo method in CollectMethods())
            {
                if (method == null || !seen.Add(method))
                {
                    continue;
                }

                object attribute = GetAttribute(method);

                if (attribute == null)
                {
                    continue;
                }

                string category = ReadText(_categoryMember, attribute);

                if (_methods.ContainsKey(method.Name))
                {
                    continue;
                }

                try
                {
                    CommandDescriptor descriptor = CommandDescriptorBuilder.FromMethod(
                        method,
                        category,
                        ReadText(_aliasMember, attribute),
                        ReadText(_descriptionMember, attribute),
                        ReadText(_exampleInputMember, attribute),
                        ReadText(_exampleOutputMember, attribute));
                    _commands.Add(MrCommandInfoFactory.FromDescriptor(descriptor));
                    _methods[method.Name] = method;
                    _descriptors[method.Name] = descriptor;
                }
                catch (Exception exception)
                {
                    failedCount++;
                    lastFailure = method.Name + ": " + exception.Message;
                }
            }

            if (failedCount > 0)
            {
                _lastError = failedCount + " comenzi nu au putut fi citite. Ultima: " + lastFailure;
            }
        }

        // ---------------------------------------------------------------- finding the commands

        /// <summary>The inspector's methods when it has any, else the scan result (cached until Refresh).</summary>
        private IEnumerable<MethodInfo> CollectMethods()
        {
            var fromInspector = new List<MethodInfo>();
            object source = InspectorCommands();

            if (source is IEnumerable pairs)
            {
                try
                {
                    foreach (object pair in pairs)
                    {
                        MethodInfo method = MethodOf(pair);

                        if (method != null)
                        {
                            fromInspector.Add(method);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _lastError = "Proxima.ProximaInspector.Commands: " + exception.Message;
                }
            }

            if (fromInspector.Count > 0)
            {
                return fromInspector;
            }

            if (_scanned == null)
            {
                _scanned = ScanAssemblies();
            }

            return _scanned;
        }

        private object InspectorCommands()
        {
            if (_inspectorType == null)
            {
                return null;
            }

            try
            {
                PropertyInfo property = _inspectorType.GetProperty("Commands", AnyStatic);

                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(null, null);
                }

                FieldInfo field = _inspectorType.GetField("Commands", AnyStatic);

                return field != null ? field.GetValue(null) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Entries of the inspector without enumerating them (this runs every frame).</summary>
        private int InspectorCount()
        {
            object source = InspectorCommands();

            if (source == null)
            {
                return 0;
            }

            if (source is ICollection collection)
            {
                return collection.Count;
            }

            try
            {
                Type type = source.GetType();

                if (_countProperty == null || _countPropertyOwner != type)
                {
                    _countProperty = type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
                    _countPropertyOwner = type;
                }

                if (_countProperty != null && _countProperty.PropertyType == typeof(int))
                {
                    return (int)_countProperty.GetValue(source, null);
                }

                int count = 0;

                if (source is IEnumerable items)
                {
                    foreach (object ignored in items)
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>The method of a KeyValuePair (or the method itself) from the inspector's entries.</summary>
        private static MethodInfo MethodOf(object entry)
        {
            if (entry == null)
            {
                return null;
            }

            if (entry is MethodInfo direct)
            {
                return direct;
            }

            Type type = entry.GetType();
            PropertyInfo value = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);

            return value != null ? value.GetValue(entry, null) as MethodInfo : null;
        }

        /// <summary>One scan of the loaded game assemblies for static methods that carry the attribute.</summary>
        private List<MethodInfo> ScanAssemblies()
        {
            var found = new List<MethodInfo>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || IsSkipped(assembly))
                {
                    continue;
                }

                Type[] types;

                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type == null)
                    {
                        continue;
                    }

                    try
                    {
                        foreach (MethodInfo method in type.GetMethods(AnyStatic | BindingFlags.DeclaredOnly))
                        {
                            if (GetAttribute(method) != null)
                            {
                                found.Add(method);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // A type that cannot be inspected holds no commands we can use.
                    }
                }
            }

            found.Sort((left, right) => string.CompareOrdinal(left.DeclaringType?.FullName + "." + left.Name, right.DeclaringType?.FullName + "." + right.Name));

            return found;
        }

        private static bool IsSkipped(Assembly assembly)
        {
            string name = assembly.GetName().Name ?? string.Empty;

            // The module's own assemblies (M3AR.*) and the system ones hold no game commands; "Proxima" itself does (built-in commands).
            foreach (string prefix in SkippedAssemblyPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- reflection helpers

        private object GetAttribute(MethodInfo method)
        {
            try
            {
                object[] attributes = method.GetCustomAttributes(_attributeType, true);

                return attributes.Length > 0 ? attributes[0] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static MemberInfo FindMember(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, AnyInstance);

            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                return property;
            }

            return type.GetField(name, AnyInstance);
        }

        /// <summary>A text member of the attribute, "" when the member is missing, renamed, not text or null.</summary>
        private static string ReadText(MemberInfo member, object attribute)
        {
            if (member == null)
            {
                return string.Empty;
            }

            try
            {
                object value = member is PropertyInfo property ? property.GetValue(attribute, null) : ((FieldInfo)member).GetValue(attribute);

                return value as string ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void ResolveLoadingReader()
        {
            _loadingResolved = true;
            _loadingReader = null;

            try
            {
                Type loader = FindType(_sceneLoaderTypeName, false);

                if (loader == null)
                {
                    return;
                }

                PropertyInfo property = loader.GetProperty("IsLoading", AnyStatic);

                if (property != null && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0 && property.CanRead)
                {
                    _loadingReader = () => (bool)property.GetValue(null, null);

                    return;
                }

                FieldInfo field = loader.GetField("IsLoading", AnyStatic);

                if (field != null && field.FieldType == typeof(bool))
                {
                    _loadingReader = () => (bool)field.GetValue(null);
                }
            }
            catch (Exception)
            {
                _loadingReader = null;
            }
        }

        /// <summary>A type by full name from any loaded assembly; optionally tries to load the Proxima assembly first when it is not loaded yet.</summary>
        internal static Type FindType(string fullName, bool loadProximaIfMissing)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            Type found = FindLoadedType(fullName);

            if (found != null || !loadProximaIfMissing)
            {
                return found;
            }

            try
            {
                Assembly.Load("Proxima");
            }
            catch (Exception)
            {
                return null;
            }

            return FindLoadedType(fullName);
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName, false);

                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (Exception)
                {
                    // An assembly that cannot answer is skipped.
                }
            }

            return null;
        }
    }
}
#endif

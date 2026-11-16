using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

namespace M3AR.LivPanel.Core
{
    public static class CommandInvoker
    {
        private const string DoneText = "OK";

        public static bool TryBuildArguments(MethodInfo method, IReadOnlyList<string> texts, out object[] values, out string error)
        {
            ParameterInfo[] parameters = method.GetParameters();
            values = new object[parameters.Length];
            error = null;

            for (int index = 0; index < parameters.Length; index++)
            {
                ParameterInfo parameter = parameters[index];
                string text = texts != null && index < texts.Count ? texts[index] : null;

                if (string.IsNullOrWhiteSpace(text))
                {
                    if (!parameter.HasDefaultValue)
                    {
                        error = "Parametrul '" + parameter.Name + "' este obligatoriu.";

                        return false;
                    }

                    values[index] = parameter.DefaultValue;

                    continue;
                }

                if (!CommandArgumentParser.TryParse(parameter.Name, parameter.ParameterType, text, out object value, out error))
                {
                    return false;
                }

                values[index] = value;
            }

            return true;
        }

        public static async Task<CommandInvocationResult> InvokeAsync(MethodInfo method, IReadOnlyList<string> texts)
        {
            if (!method.IsStatic)
            {
                return CommandInvocationResult.Failed("Comanda '" + method.Name + "' nu e statica.");
            }

            if (!TryBuildArguments(method, texts, out object[] values, out string error))
            {
                return CommandInvocationResult.Failed(error);
            }

            try
            {
                object returned = method.Invoke(null, values);
                object result = await AwaitResult(method.ReturnType, returned).ConfigureAwait(false);

                return CommandInvocationResult.Succeeded(FormatResult(result));
            }
            catch (TargetInvocationException exception)
            {
                return CommandInvocationResult.Failed(Describe(exception.InnerException ?? exception));
            }
            catch (Exception exception)
            {
                return CommandInvocationResult.Failed(Describe(exception));
            }
        }

        private static async Task<object> AwaitResult(Type returnType, object returned)
        {
            if (returned == null)
            {
                return null;
            }

            if (returned is Task task)
            {
                await task.ConfigureAwait(false);

                return returnType.IsGenericType ? returnType.GetProperty("Result")?.GetValue(task) : null;
            }

            MethodInfo asTask = returnType.GetMethod("AsTask", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

            if (asTask != null && typeof(Task).IsAssignableFrom(asTask.ReturnType))
            {
                var converted = (Task)asTask.Invoke(returned, null);
                await converted.ConfigureAwait(false);

                return asTask.ReturnType.IsGenericType ? asTask.ReturnType.GetProperty("Result")?.GetValue(converted) : null;
            }

            return returned;
        }

        private static string FormatResult(object result)
        {
            string text = result == null ? null : Convert.ToString(result, CultureInfo.InvariantCulture);

            return string.IsNullOrEmpty(text) ? DoneText : text;
        }

        private static string Describe(Exception exception)
        {
            return exception.GetType().Name + ": " + exception.Message;
        }
    }
}

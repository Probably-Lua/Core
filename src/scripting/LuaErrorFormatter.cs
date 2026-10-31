using System;
using System.Reflection;
using MoonSharp.Interpreter;

namespace ProbablyLua.Api
{
    internal static class LuaErrorFormatter
    {
        // MoonSharp provides source-file/line diagnostics separately from
        // Exception.ToString(), which otherwise exposes mostly C# internals.
        public static string Describe(Exception exception)
        {
            while (exception is TargetInvocationException && exception.InnerException != null)
                exception = exception.InnerException;

            if (exception is InterpreterException luaError)
                return string.IsNullOrWhiteSpace(luaError.DecoratedMessage)
                    ? luaError.Message
                    : luaError.DecoratedMessage;

            if (exception is ArgumentException || exception is InvalidOperationException || exception is TimeoutException)
                return exception.Message;

            return $"Host error ({exception.GetType().Name}): {exception.Message}\n{exception.StackTrace}";
        }

        public static string Format(string operation, Exception exception) =>
            $"{operation} failed.\n  {Describe(exception).Replace("\n", "\n  ")}";
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ExceptionHelper
{
    public static class Throw
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArgumentOutOfRangeException(string callerName, object? actualValue, string message)
        {
            throw new ArgumentOutOfRangeException($"{callerName} : argument = {actualValue}; {message}");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void IndexOutOfRangeException(string callerName, object? actualValue)
        {
            throw new IndexOutOfRangeException($"{callerName} : index = {actualValue}");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CollectionOverloadException(string callerName)
        {
            throw new InvalidOperationException($"{callerName} : Operation would result in a collection overflow.");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArgumentNullException(string callerName, string objectName)
        {
            throw new ArgumentNullException(objectName, callerName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArgumentException(string callerName, string message)
        {
            throw new ArgumentException($"{callerName} : {message}");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void InvalidOperationException(string callerName, string message)
        {
            throw new InvalidOperationException($"{callerName} : {message}");
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArrayWasChanged(string callerName)
        {
            throw new InvalidOperationException($"{callerName} : Array was changed");
        }
    }
}

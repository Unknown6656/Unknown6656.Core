using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System;

namespace Unknown6656.Common
{
    public static unsafe class FunctionExtensions
    {
        public static void Do(Action? func) => func?.Invoke();

        public static T Do<T>(Func<T> func) => func.Invoke();

        public static unsafe void Do(delegate*<void> func) => func();

        public static unsafe T Do<T>(delegate*<T> func) => func();

        public static void TryDo(Action? func)
        {
            try
            {
                if (func is { } f)
                    f();
            }
            catch
            {
            }
        }

        public static T TryDo<T>(Func<T> func, T default_value)
        {
            try
            {
                if (func is { } f)
                    return f();
            }
            catch
            {
            }

            return default_value;
        }

        public static unsafe void TryDo(delegate*<void> func)
        {
            try
            {
#pragma warning disable IDE1005
                if (func != null)
                    func();
#pragma warning restore
            }
            catch
            {
            }
        }

        public static unsafe T TryDo<T>(delegate*<T> func, T default_value)
        {
            try
            {
                if (func != null)
                    return func();
            }
            catch
            {
            }

            return default_value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T TryAll<T>(params Func<T>[] functions)
        {
            List<Exception> exceptions = new List<Exception>();

            foreach (Func<T> f in functions)
                try
                {
                    return f();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

            throw new AggregateException(exceptions.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FromFunctionPointer<T>(void* pointer) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>((nint)pointer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* FromFunctionPointer<T>(this T @delegate) where T : Delegate => (void*)Marshal.GetFunctionPointerForDelegate(@delegate);
    }
}

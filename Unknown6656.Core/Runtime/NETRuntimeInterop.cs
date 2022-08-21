using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Unknown6656.Runtime;


public static unsafe class NETRuntimeInterop
{
    public static void* GetHeapPointer(object? @object) => @object is null ? (void*)null : *(void**)Unsafe.AsPointer(ref @object);

    public static Win32Exception GetLastWin32Error() => new Win32Exception(Marshal.GetLastWin32Error());
}

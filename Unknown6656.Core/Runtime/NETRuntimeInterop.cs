using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System;

namespace Unknown6656.Runtime;


public static unsafe class NETRuntimeInterop
{
    public static void* GetHeapPointer(object? @object) => @object is null ? (void*)null : *(void**)Unsafe.AsPointer(ref @object);
}

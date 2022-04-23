using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Unknown6656.Runtime;


public static unsafe class NETRuntimeInterop
{
    public static void* GetHeapPointer(object? @object) => @object is null ? (void*)null : *(void**)Unsafe.AsPointer(ref @object);
}

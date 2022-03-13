using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

using Unknown6656.Controls.Console;

namespace Unknown6656.Runtime;


internal static unsafe class NativeInterop
{
    [DllImport("ntdll.dll")]
    [SupportedOSPlatform(OS.WIN)]
    public static extern int RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool _);

    [DllImport("ntdll.dll")]
    [SupportedOSPlatform(OS.WIN)]
    public static extern int NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, void* Parameters, uint ValidResponseOption, out uint _);

    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool CreateHardLink([MarshalAs(UnmanagedType.LPWStr)]  string lpFileName, [MarshalAs(UnmanagedType.LPWStr)] string lpExistingFileName, void* reserved);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern int GetShortPathName([MarshalAs(UnmanagedType.LPWStr)] string? path, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder @short, int length);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, EntryPoint = "GetCurrentObject", ExactSpelling = true, SetLastError = true)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern nint IntGetCurrentObject(HandleRef hDC, nint uObjectType);

    [DllImport("kernel32.dll", SetLastError = true)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern void* GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool SetConsoleMode(void* hWnd, ConsoleMode dwMode);

    [DllImport("kernel32.dll")]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool GetConsoleMode(void* hWnd, ConsoleMode* lpMode);

    [DllImport("kernel32.dll")]
    [SupportedOSPlatform(OS.WIN)]
    public static extern void* VirtualAlloc(void* addr, int size, int type, int protect);

    [DllImport("kernel32.dll")]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool VirtualProtect(void* addr, int size, int new_protect, int* old_protect);

    [DllImport("kernel32.dll")]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool VirtualFree(void* addr, int size, int type);

    [DllImport("libc.so")]
    [SupportedOSPlatform(OS.LIN)]
    [SupportedOSPlatform(OS.MAC)]
    public static extern void mprotect(void* buffer, int size, int mode);

    [DllImport("libc.so")]
    [SupportedOSPlatform(OS.LIN)]
    [SupportedOSPlatform(OS.MAC)]
    public static extern void posix_memalign(void** buffer, int alignment, int size);

    [DllImport("libc.so")]
    [SupportedOSPlatform(OS.LIN)]
    [SupportedOSPlatform(OS.MAC)]
    public static extern void free(void* buffer);
}

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

using Unknown6656.Controls.Console;
using Unknown6656.Imaging;

namespace Unknown6656.Runtime;


internal static unsafe class NativeInterop
{
    public const string GDI32 = "gdi32.dll";
    public const string USER32 = "user32.dll";
    public const string KERNEL32 = "kernel32.dll";
    public const string NTDLL = "ntdll.dll";
    public const string LIBC = "libc.so";


    [DllImport(USER32)]
    [SupportedOSPlatform(OS.WIN)]
    public extern static int GetDC(void* hwnd);

    [DllImport(USER32)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool GetClientRect(void* hWnd, out RECT lpRect);

    [DllImport(USER32)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool GetWindowRect(void* hWnd, out RECT lpRect);


    [DllImport(GDI32, CharSet = CharSet.Auto, EntryPoint = "GetCurrentObject", ExactSpelling = true, SetLastError = true)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern nint IntGetCurrentObject(HandleRef hDC, nint uObjectType);


    [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern int GetShortPathName([MarshalAs(UnmanagedType.LPWStr)] string? path, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder @short, int length);

    [DllImport(KERNEL32, CharSet = CharSet.Unicode)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool CreateHardLink([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, [MarshalAs(UnmanagedType.LPWStr)] string lpExistingFileName, void* reserved);

    [DllImport(KERNEL32, SetLastError = true)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern void* GetStdHandle(int nStdHandle);

    [DllImport(KERNEL32)]
    [SupportedOSPlatform(OS.WIN)]
    public extern static void* GetConsoleWindow();

    [DllImport(KERNEL32)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool SetConsoleMode(void* hWnd, ConsoleMode dwMode);

    [DllImport(KERNEL32)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool GetConsoleMode(void* hWnd, ConsoleMode* lpMode);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetCurrentConsoleFontEx(void* hWnd, bool MaximumWindow, ref ConsoleFontInfo ConsoleCurrentFontEx);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool GetCurrentConsoleFontEx(void* hWnd, bool MaximumWindow, ref ConsoleFontInfo ConsoleCurrentFontEx);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool ReadConsoleInput(nint hConsoleInput, [Out] INPUT_RECORD[] lpBuffer, int nLength, out int lpNumberOfEventsRead);

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport(KERNEL32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool WriteConsoleInput(nint hConsoleInput, INPUT_RECORD[] lpBuffer, int nLength, out int lpNumberOfEventsWritten);

    [DllImport(KERNEL32)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern void* VirtualAlloc(void* addr, int size, int type, int protect);

    [DllImport(KERNEL32)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool VirtualProtect(void* addr, int size, int new_protect, int* old_protect);

    [DllImport(KERNEL32)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern bool VirtualFree(void* addr, int size, int type);


    [DllImport(NTDLL)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern int RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool _);

    [DllImport(NTDLL)]
    [SupportedOSPlatform(OS.WIN)]
    public static extern int NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, void* Parameters, uint ValidResponseOption, out uint _);


    [DllImport(LIBC)]
    [SupportedOSPlatform(OS.LIN)]
    [SupportedOSPlatform(OS.MAC)]
    public static extern void mprotect(void* buffer, int size, int mode);

    [DllImport(LIBC)]
    [SupportedOSPlatform(OS.LIN)]
    [SupportedOSPlatform(OS.MAC)]
    public static extern void posix_memalign(void** buffer, int alignment, int size);

    [DllImport(LIBC)]
    [SupportedOSPlatform(OS.LIN)]
    [SupportedOSPlatform(OS.MAC)]
    public static extern void free(void* buffer);
}

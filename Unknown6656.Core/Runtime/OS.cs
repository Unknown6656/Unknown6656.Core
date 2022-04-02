using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Unknown6656.Runtime;


public static class OS
{
    public const string WIN = "windows";
    public const string LIN = "linux";
    public const string IOS = "iOS";
    public const string MAC = "macos";
    public const string CAT = "MacCatalyst";
    public const string AND = "android";

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static bool IsFreeBSD => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public static bool IsPosix => IsWindows | IsOSX | IsFreeBSD;


    /// <summary>
    /// Executes the given bash command
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    [SupportedOSPlatform(LIN)]
    [SupportedOSPlatform(MAC)]
    public static string? ExecutBashCommand(string command)
    {
        using Process? process = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = false,
        });
        string? result = process?.StandardOutput.ReadToEnd();

        process?.WaitForExit();

        return result;
    }

    [SupportedOSPlatform(LIN)]
    [SupportedOSPlatform(MAC)]
    [SupportedOSPlatform(WIN)]
    public static unsafe void CreateBluescreenOfDeath()
    {
#pragma warning disable CA1416 // Validate platform compatibility
        if (OS.IsWindows)
        {
            NativeInterop.RtlAdjustPrivilege(19, true, false, out _);
            NativeInterop.NtRaiseHardError(0xc0000420u, 0, 0, null, 6, out _);
        }
        else
        {
            ExecutBashCommand("echo 1 > /proc/sys/kernel/sysrq");
            ExecutBashCommand("echo c > /proc/sysrq-trigger");
        }
#pragma warning restore CA1416
    }
}

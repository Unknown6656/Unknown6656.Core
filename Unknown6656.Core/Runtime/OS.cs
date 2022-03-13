using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Unknown6656.Runtime;


public static class OS
{
    internal const string WIN = "windows";
    internal const string LIN = "linux";
    internal const string IOS = "iOS";
    internal const string MAC = "macos";
    internal const string CAT = "MacCatalyst";
    internal const string AND = "android";


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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

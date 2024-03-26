using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.IO;

namespace Unknown6656.Runtime;


public static class OS
{
    private const string DOCKER_INDICATOR = "/.dockerenv";
    private const string WSL_INDICATOR = "/proc/sys/fs/binfmt_misc/WSLInterop";

    internal const string WIN = "windows";
    internal const string LIN = "linux";
    internal const string IOS = "iOS";
    internal const string MAC = "macos";
    internal const string CAT = "MacCatalyst";
    internal const string AND = "android";


    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    public static bool IsFreeBSD => RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

    public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public static bool IsPosix => IsWindows | IsOSX | IsFreeBSD;

    public static bool IsInsideDocker => File.Exists(DOCKER_INDICATOR);


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
        if (IsWindows)
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

    // TODO : check if is running inside a container (container, snap, etc.)
    // TODO : check if is running inside a VM (vmware, virtualbox, etc.)



    // TODO : check if the current execution context is WSL
    public static bool IsInsideWSL
    {
        get
        {
            if (File.Exists(WSL_INDICATOR))
                return true;

            // TODO : check "uname -a" for "microsoft" substring.

            return false;
        }
    }
}

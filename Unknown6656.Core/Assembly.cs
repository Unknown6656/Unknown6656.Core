using System.Runtime.CompilerServices;
using System;

using Unknown6656.Runtime;

[assembly: CLSCompliant(false)]


internal class __module
{
#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
    public static void Initializer()
    {
        if (!LibGDIPlusInstaller.IsGDIInstalled)
            LibGDIPlusInstaller.TryInstallGDI();
    }
#pragma warning restore CA2255
}

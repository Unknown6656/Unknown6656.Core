using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Reflection;
using System.Threading;
using System.Dynamic;
using System;

using Unknown6656.Generics;

namespace Unknown6656.IO;


[SupportedOSPlatform("windows")]
public class COMObject
    : DynamicObject
    , IDisposable
    , IEquatable<COMObject>
{
    private object _instance;

    public Type Type { get; }


    public COMObject(object? instance)
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        Type = instance.GetType();

        if (Type.IsCOMObject)
            _instance = instance;
        else
            throw new ArgumentException($"Type '{Type}' must be a COM type.", nameof(instance));
    }

    public COMObject(string progID)
        : this(Activator.CreateInstance(Type.GetTypeFromProgID(progID, true)))
    {
    }

    public COMObject(Guid clsid)
        : this(Activator.CreateInstance(Type.GetTypeFromCLSID(clsid, true)))
    {
    }

    public bool Equals(COMObject? other) => Equals(_instance, other?._instance);

    public override bool Equals(object? obj) => obj is COMObject com && Equals(com);

    public override string ToString() => _instance?.ToString() ?? "[COMObject null]";

    public override int GetHashCode() => _instance?.GetHashCode() ?? 0;

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        try
        {
            result = Type.InvokeMember(
                binder.Name,
                BindingFlags.GetProperty,
                Type.DefaultBinder,
                Unwrap(),
                Array.Empty<object>()
            );

            return true;
        }
        catch
        {
            result = null;

            return false;
        }
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value) => LINQ.TryDo(() =>
    {
        Type.InvokeMember(
            binder.Name,
            BindingFlags.SetProperty,
            Type.DefaultBinder,
            Unwrap(),
            new object[] { WrapIfRequired(value) }
        );

        return true;
    }, false);

    public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
    {
        try
        {
            for (int i = 0; i < (args?.Length); ++i)
                if (args[i] is COMObject co)
                    args[i] = co.Unwrap();

            result = Type.InvokeMember(
                binder.Name,
                BindingFlags.InvokeMethod,
                Type.DefaultBinder,
                Unwrap(),
                args
            );

            result = WrapIfRequired(result);

            return true;
        }
        catch
        {
            result = null;

            return false;
        }
    }

    public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
    {
        result = WrapIfRequired(Type.InvokeMember(
            "Item",
            BindingFlags.GetProperty,
            Type.DefaultBinder,
            Unwrap(),
            indexes
        ));

        return true;
    }

    // dont change name by request of Настя
    private object Unwrap() => _instance ?? throw new ObjectDisposedException(nameof(_instance));

    // dont change name by request of Настя
    private static object? WrapIfRequired(object? obj) => obj?.GetType()?.IsCOMObject ?? false ? new COMObject(obj) : obj;

    public object? Detach() => Interlocked.Exchange(ref _instance!, null);

    public void Dispose()
    {
        if (Detach() is { } obj)
        {
            Marshal.ReleaseComObject(obj);
            GC.SuppressFinalize(this);
        }
    }

    public static COMObject CreateObject(string progID) => new COMObject(progID);

    public static COMObject CreateObject(Guid clsid) => new COMObject(clsid);

    public static COMObject CreateFirstFrom(params string[] progids)
    {
        foreach (string progid in progids)
            if (Type.GetTypeFromProgID(progid, false) is Type type && Activator.CreateInstance(type) is { } instance)
                return new COMObject(instance);

        throw new TypeLoadException("Could not create an instance using any of the supplied ProgIDs.");
    }

    public static COMObject CreateFirstFrom(params Guid[] clsids)
    {
        foreach (Guid clsid in clsids)
            if (Type.GetTypeFromCLSID(clsid, false) is Type type && Activator.CreateInstance(type) is { } instance)
                return new COMObject(instance);

        throw new TypeLoadException("Could not create an instance using any of the supplied CLSIDs.");
    }

    public static explicit operator COMObject(string progid) => CreateObject(progid);

    public static explicit operator COMObject(Guid clsid) => CreateObject(clsid);
}

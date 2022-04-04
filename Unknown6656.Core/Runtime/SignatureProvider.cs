using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Unknown6656.Common;
using Unknown6656.Generics;

namespace Unknown6656.Runtime;


public abstract class SignatureProvider
{
    public SignatureOptions Options { get; init; } = SignatureOptions.Default;


    protected abstract string GetTypeName(Type? type);

    protected abstract string GetValueRepresentation(object? value, Type type);

    protected abstract List<string> GetAttributes(IEnumerable<CustomAttributeData> attributes);

    public abstract string GenerateSignature(MemberInfo member);

    public string GenerateTypeSignature(Type type) => GenerateSignature(type);

    public string GenerateTypeSignature<T>() => GenerateTypeSignature(typeof(T));

    public string GenerateTypeSignature<T>(T _) => GenerateTypeSignature(typeof(T));

    public string GenerateMethodSignature(Delegate method) => GenerateSignature(method.GetMethodInfo());
}

public class CSharpSignatureProvider
    : SignatureProvider
{
    private static readonly Regex REGEX_GENTPE = new(@"`\d+\b", RegexOptions.Compiled);


    private static string GetModifiers(MethodAttributes attributes) => (from p in new[]
                                                                                  {
                                                                                      (MethodAttributes.Private, "private"),
                                                                                      (MethodAttributes.FamANDAssem, "internal protected"),
                                                                                      (MethodAttributes.Assembly, "internal"),
                                                                                      (MethodAttributes.Family, "protected"),
                                                                                      (MethodAttributes.FamORAssem, "private protected"),
                                                                                      (MethodAttributes.Public, "public"),
                                                                                      (MethodAttributes.Static, "static"),
                                                                                      (MethodAttributes.Final, "sealed"),
                                                                                      (MethodAttributes.Abstract, "abstract"),
                                                                                      (MethodAttributes.PinvokeImpl, "extern"),
                                                                                  }
                                                                                  where attributes.HasFlag(p.Item1)
                                                                                  select p.Item2).StringJoin(" ");

    private static string GetModifiers(FieldAttributes attributes) => (from p in new[]
                                                                                 {
                                                                                     (FieldAttributes.Private, "private"),
                                                                                     (FieldAttributes.FamANDAssem, "internal protected"),
                                                                                     (FieldAttributes.Assembly, "internal"),
                                                                                     (FieldAttributes.Family, "protected"),
                                                                                     (FieldAttributes.FamORAssem, "private protected"),
                                                                                     (FieldAttributes.Public, "public"),
                                                                                     (FieldAttributes.Static, "static"),
                                                                                     (FieldAttributes.InitOnly, "readonly"),
                                                                                     (FieldAttributes.Literal, "const"),
                                                                                     (FieldAttributes.PinvokeImpl, "extern"),
                                                                                 }
                                                                                 where attributes.HasFlag(p.Item1)
                                                                                 select p.Item2).StringJoin(" ");

    private static string GetLiteral(char character) => character switch
    {
        '\"' => @"\""",
        '\\' => @"\\",
        '\0' => @"\0",
        '\a' => @"\a",
        '\b' => @"\b",
        '\f' => @"\f",
        '\n' => @"\n",
        '\r' => @"\r",
        '\t' => @"\t",
        '\v' => @"\v",
        >= '\x20' and < '\x7f' => character.ToString(),
        _ => $"\\u{character:x4}",
    };

    private string GetConstructorTypeName(ConstructorInfo constructor)
    {
        Type? type = constructor.ReflectedType ?? constructor.DeclaringType;
        string name = type?.Name ?? "?";

        if (type?.IsGenericType is true && name.Match(REGEX_GENTPE, out Match? m))
            name = name[..m.Index] + name[(m.Index + m.Length)..];

        return name;
    }

    protected override string GetTypeName(Type? type)
    {
        string? ns = type?.DeclaringType is Type parent ? GetTypeName(parent) : (type?.Namespace);

        if (!Options.Compact)
            ns ??= "global::";

        if (ns is { })
            ns += '.';

        if (type is null)
            return "?";
        else if (type == typeof(void))
            return "void";
        else if (type == typeof(byte))
            return "byte";
        else if (type == typeof(sbyte))
            return "sbyte";
        else if (type == typeof(short))
            return "short";
        else if (type == typeof(ushort))
            return "ushort";
        else if (type == typeof(char))
            return "char";
        else if (type == typeof(int))
            return "int";
        else if (type == typeof(uint))
            return "uint";
        else if (type == typeof(nint))
            return "nint";
        else if (type == typeof(nuint))
            return "nuint";
        else if (type == typeof(long))
            return "long";
        else if (type == typeof(ulong))
            return "ulong";
        else if (type == typeof(float))
            return "float";
        else if (type == typeof(double))
            return "double";
        else if (type == typeof(decimal))
            return "decimal";
        else if (type == typeof(string))
            return "string";
        else if (type == typeof(object))
            return "object";
        else if (type.IsArray)
            return $"{GetTypeName(type.GetElementType())}[{new string(',', type.GetArrayRank() - 1)}]";
        else if (type.IsPointer)
            return $"{GetTypeName(type.GetElementType())}*";
        else if (type.IsByRef)
            return $"ref {GetTypeName(type.GetElementType())}";
        else
        {
            string name = type.Name;
            string? suffix = null;

            if (type.IsGenericType)
            {
                string genargs = type.GenericTypeArguments.Select(GetTypeName).StringJoin(", ");

                if (name.Match(REGEX_GENTPE, out Match? m))
                    name = name[..m.Index] + name[(m.Index + m.Length)..];

                suffix = $"<{genargs}>";
            }

            // TODO : ?

            return $"{ns}{name}{suffix}";
        }
    }

    protected override string GetValueRepresentation(object? value, Type type) => value switch
    {
        null => "null",
        byte x => x.ToString(),
        sbyte x => x.ToString(),
        short x => x.ToString(),
        ushort x => x.ToString(),
        char x => GetLiteral(x),
        int x => x.ToString(),
        uint x => x.ToString(),
        nint x => $"0x{(long)x:x16}",
        nuint x => $"0x{(ulong)x:x16}",
        long x => x.ToString(),
        ulong x => x.ToString(),
        float x => x.ToString(),
        double x => x.ToString(),
        decimal x => x.ToString(),
        Type x => $"typeof({GetTypeName(x)})",
        DateTime x => $"new {GetTypeName(typeof(DateTime))}({x.Ticks})",
        TimeSpan x => $"new {GetTypeName(typeof(TimeSpan))}({x.Ticks})",
        string x => $"\"{x.Select(GetLiteral).StringConcat()}\"",
        // Array x => ,
        _ => throw new NotImplementedException(),
    };

    protected override List<string> GetAttributes(IEnumerable<CustomAttributeData> attributes)
    {
        List<string> attrs = new();

        foreach (CustomAttributeData attr in attributes)
        {
            string name = GetTypeName(attr.AttributeType);
            List<string> args = new();

            if (name.EndsWith("Attribute"))
                name = name[..^"Attribute".Length];

            foreach (CustomAttributeTypedArgument arg in attr.ConstructorArguments)
                args.Add(GetValueRepresentation(arg.Value, arg.ArgumentType));

            foreach (CustomAttributeNamedArgument arg in attr.NamedArguments)
                args.Add($"{arg.MemberName} = {GetValueRepresentation(arg.TypedValue.Value, arg.TypedValue.ArgumentType)}");

            attrs.Add($"[{name}{(args.Count > 0 ? $"({args.StringJoin(", ")})" : "")}]");
        }

        return attrs;
    }


    private List<string> GetParameters(IEnumerable<ParameterInfo> parameters)
    {
        List<string> ps = new();

        foreach (ParameterInfo param in parameters.OrderBy(p => p.Position))
        {
            List<string>? p_attrs = GetAttributes(param.CustomAttributes);
            string p_type = GetTypeName(param.ParameterType);
            string p_name = param.Name ?? "_";

            if (param.Attributes.HasFlag(ParameterAttributes.Retval))
                p_attrs.Add("[RetVal]");

            List<string> p_mod = new();

            if (param.Attributes.HasFlag(ParameterAttributes.In))
                p_mod.Add("in ");
            if (param.Attributes.HasFlag(ParameterAttributes.Out))
                p_mod.Add("out ");

            string p_value = param.HasDefaultValue && param.Attributes.HasFlag(ParameterAttributes.Optional)
                           ? " = " + GetValueRepresentation(param.RawDefaultValue, param.ParameterType) : "";

            ps.Add($"{p_attrs.Select(p => p + ' ').StringConcat()}{p_mod}{p_type} {p_name}{p_value}");
        }

        return ps;
    }

    public string GenerateSignature(MethodBase method)
    {
        List<string> attributes = GetAttributes(method.CustomAttributes);
        string modifiers = GetModifiers(method.Attributes);
        Type? container = method.DeclaringType;
        Type? rettype = null;
        string name;

        if (container != (method as MethodInfo)?.GetBaseDefinition()?.DeclaringType)
            modifiers += " override";

        List<string> parameters = ;

        method.CallingConvention;
        method.MethodImplementationFlags;

        if (method is ConstructorInfo constructor)
        {
            name = GetConstructorTypeName(constructor);



        }
        else if (method is MethodInfo function)
        {
            name = function.Name;
            rettype = function.ReturnType;

            function.ContainsGenericParameters;
            function.IsConstructedGenericMethod;
            function.IsGenericMethod;

            function.ReturnTypeCustomAttributes;

        }
        else
            throw new NotImplementedException();
    }

    public string GenerateSignature(FieldInfo field)
    {
        string modifiers = GetModifiers(field.Attributes);
        string? value = field.IsLiteral ? $" = {GetValueRepresentation(field.GetRawConstantValue(), field.FieldType)}" : null;


        throw new NotImplementedException();
    }

    public override string GenerateSignature(MemberInfo member)
    {
        List<string> attrs = GetAttributes(member);


        string signature;

        switch (member)
        {
            case FieldInfo fi:
                signature = GenerateSignature(fi);
                break;
            case MethodBase mi:
                signature = GenerateSignature(mi);
                break;
            case EventInfo ei:
                var add = ei.AddMethod;
                var rem = ei.RemoveMethod;



                break;
            case PropertyInfo pi:

                break;
            case Type ti:

                break;
        }




        throw new NotImplementedException();





        //    UnmanagedExport

        //    CheckAccessOnOverride
        //    HasSecurity
        //    RequireSecObject





        //method.Attributes;
        //method.ReturnType;




    }
}

public record SignatureOptions
{
    public static SignatureOptions Default { get; } = new();

    public int IndentationLevel { get; init; } = 0;
    public bool Compact { get; init; } = false;
    public bool AppendSemicolon { get; init; } = false;
    public bool FullyQualifiedTypeNames { get; init; } = true;
}

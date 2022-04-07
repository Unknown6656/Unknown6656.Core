using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

using Unknown6656.Common;
using Unknown6656.Generics;

namespace Unknown6656.Runtime;


public abstract class SignatureProvider
{
    public SignatureOptions Options { get; init; } = SignatureOptions.Default;


    protected abstract string GetTypeName(Type? type, bool[]? nullability_annotations = null);

    protected abstract string GetValueRepresentation(object? value, Type type);

    protected abstract string GetAttribute(CustomAttributeData attribute, bool return_params = false);

    protected abstract string GetParameter(ParameterInfo parameter, bool extension_parameter = false);

    protected List<string> GetAttributes(IEnumerable<CustomAttributeData>? attributes, bool return_params = false)
    {
        List<string> attrs = new();

        if (attributes is { })
            foreach (CustomAttributeData attr in attributes)
                attrs.Add(GetAttribute(attr, return_params));

        return attrs;
    }

    protected List<string> GetParameters(IEnumerable<ParameterInfo>? parameters, bool extension_method = false)
    {
        List<string> ps = new();

        if (parameters is { })
            foreach (ParameterInfo param in parameters.OrderBy(p => p.Position))
                ps.Add(GetParameter(param, extension_method && ps.Count is 0));

        return ps;
    }

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
    private const string TYPE_NULLABLE_ATTRIBUTE = "System.Runtime.CompilerServices.NullableAttribute";
    private const char TYPE_DELIMITER = '.';


    protected static string GetModifiers(MethodAttributes attributes)
    {
        string visibility = (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Private or MethodAttributes.PrivateScope => "private",
            MethodAttributes.FamANDAssem => "internal protected",
            MethodAttributes.Assembly => "internal",
            MethodAttributes.Family => "protected",
            MethodAttributes.FamORAssem => "private protected",
            MethodAttributes.Public => "public",
        };

        string modifiers = (from p in new[]
                            {
                                (MethodAttributes.Static, "static"),
                                (MethodAttributes.Final, "sealed"),
                                (MethodAttributes.Abstract, "abstract"),
                                (MethodAttributes.PinvokeImpl, "extern"),
                            }
                            where attributes.HasFlag(p.Item1)
                            select ' ' + p.Item2).Distinct().StringConcat();

        return visibility + modifiers;
    }

    protected static string GetModifiers(FieldAttributes attributes)
    {
        string visibility = (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Private or FieldAttributes.PrivateScope => "private",
            FieldAttributes.FamANDAssem => "internal protected",
            FieldAttributes.Assembly => "internal",
            FieldAttributes.Family => "protected",
            FieldAttributes.FamORAssem => "private protected",
            FieldAttributes.Public => "public",
        };

        string modifiers = (from p in new[]
                            {
                                (FieldAttributes.Static, "static"),
                                (FieldAttributes.InitOnly, "readonly"),
                                (FieldAttributes.Literal, "const"),
                                (FieldAttributes.PinvokeImpl, "extern"),
                            }
                            where attributes.HasFlag(p.Item1)
                            select ' ' + p.Item2).Distinct().StringConcat();

        return visibility + modifiers;
    }

    protected static string GetLiteral(char character) => character switch
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

    protected string GetConstructorTypeName(ConstructorInfo constructor)
    {
        Type? type = constructor.ReflectedType ?? constructor.DeclaringType;
        string name = type?.Name ?? "?";

        if (type?.IsGenericType is true && name.Match(REGEX_GENTPE, out Match? m))
            name = name[..m.Index] + name[(m.Index + m.Length)..];

        return name;
    }

    protected string GetTypeName(Type? type) => GetTypeName(type, null);

    protected override string GetTypeName(Type? type, bool[]? nullability_annotations)
    {
        string? ns = type?.DeclaringType is Type parent ? GetTypeName(parent) : (type?.Namespace);

        ns = ns?.Replace(Type.Delimiter, TYPE_DELIMITER);

        if (!Options.Compact)
            ns ??= "global::";

        if (ns is { })
            ns += TYPE_DELIMITER;

        if (type?.IsGenericParameter is true)
            ns = "";

        nullability_annotations ??= new[] { false };

        if (type is null)
            return "<error-type>";
        else if (type == typeof(bool))
            return "bool";
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
            return nullability_annotations.Any(LINQ.id) ? "string?" : "string";
        else if (type == typeof(object))
            return nullability_annotations.Any(LINQ.id) ? "object?" : "object";
        else if (type.IsPointer)
            return $"{GetTypeName(type.GetElementType(), nullability_annotations)}*";
        else if (type.IsByRef)
            return $"ref {GetTypeName(type.GetElementType(), nullability_annotations)}";
        else if (type.IsArray)
            return $"{GetTypeName(type.GetElementType())}[{new string(',', type.GetArrayRank() - 1)}]"; // nullable
        else
        {
            string name = type.Name;
            string? suffix = null;

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    return $"{GetTypeName(type.GenericTypeArguments[0])}?";

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
        _ when type.IsEnum => LINQ.Do(delegate
        {
            string @enum = GetTypeName(type);

            return Enum.GetName(type, value) is string name ? $"{@enum}.{name}" : $"({@enum}){(long)(dynamic)value}";
        }),
        true => "true",
        false => "false",
        byte x => $"(byte){x}",
        sbyte x => $"(sbyte){x}",
        short x => $"(short){x}",
        ushort x => $"(ushort){x}",
        char x => GetLiteral(x),
        int x => x.ToString(),
        uint x => $"{x}u",
        nint x => $"0x{(long)x:x16}",
        nuint x => $"0x{(ulong)x:x16}",
        long x => $"{x}L",
        ulong x => $"{x}UL",
        float x => $"{x}f",
        double x => $"{x}d",
        decimal x => $"{x}m",
        Type x => $"typeof({GetTypeName(x)})",
        DateTime x => $"new {GetTypeName(typeof(DateTime))}({x.Ticks})",
        TimeSpan x => $"new {GetTypeName(typeof(TimeSpan))}({x.Ticks})",
        string x => $"\"{x.Select(GetLiteral).StringConcat()}\"",

        // Array x => ,
        _ => throw new NotImplementedException(),
    };

    protected override string GetAttribute(CustomAttributeData attribute, bool return_params = false)
    {
        string name = GetTypeName(attribute.AttributeType);
        List<string> args = new();

        if (name.EndsWith("Attribute"))
            name = name[..^"Attribute".Length];

        foreach (CustomAttributeTypedArgument arg in attribute.ConstructorArguments)
            args.Add(GetValueRepresentation(arg.Value, arg.ArgumentType));

        foreach (CustomAttributeNamedArgument arg in attribute.NamedArguments)
            args.Add($"{arg.MemberName} = {GetValueRepresentation(arg.TypedValue.Value, arg.TypedValue.ArgumentType)}");

        return $"[{(return_params ? "return: " : "")}{name}{(args.Count > 0 ? $"({args.StringJoin(", ")})" : "")}]";
    }

    protected (List<CustomAttributeData> attributes, string type_name) ProcessNullabilityInfo(IEnumerable<CustomAttributeData> attributes, Type type)
    {
        List<CustomAttributeData> attrs = new();
        string? name = null;

        foreach (CustomAttributeData attribute in attributes)
            if (attribute.AttributeType is Type tattr && (tattr.FullName ?? tattr.AssemblyQualifiedName ?? tattr.Name)
                .Replace(Type.Delimiter, TYPE_DELIMITER).Contains(TYPE_NULLABLE_ATTRIBUTE, StringComparison.InvariantCultureIgnoreCase))
            {
                IList<CustomAttributeTypedArgument> rawargs = attribute.ConstructorArguments;
                bool[] args = (rawargs.All(a => a.ArgumentType == typeof(byte))
                             ? rawargs.Select(a => (byte)a.Value!)
                             : rawargs.All(a => a.ArgumentType == typeof(byte[]))
                             ? rawargs.SelectMany(a => a.Value as byte[] ?? Array.Empty<byte>())
                             : throw new NotImplementedException()).ToArray(b => b is 2);

                if (args.Length > 0 && args.Any(LINQ.id))
                    name = GetTypeName(type, args);
            }
            else
                attrs.Add(attribute);

        return (attrs, name ?? GetTypeName(type));
    }

    protected override string GetParameter(ParameterInfo parameter, bool extension_parameter = false)
    {
        List<CustomAttributeData> attributes = parameter.CustomAttributes.ToList();

        (attributes, string p_type) = ProcessNullabilityInfo(attributes, parameter.ParameterType);

        List<string>? p_attrs = GetAttributes(attributes);
        string p_name = parameter.Name ?? "_";

        foreach (Type mod in parameter.GetOptionalCustomModifiers().Concat(parameter.GetRequiredCustomModifiers()))
            p_attrs.Add($"[{GetTypeName(mod)}]");

        if (parameter.Attributes.HasFlag(ParameterAttributes.Retval))
            p_attrs.Add("[RetVal]");

        string p_mod = extension_parameter ? "this " : "";

        if (parameter.Attributes.HasFlag(ParameterAttributes.In))
            p_mod = "in " + p_mod;
        if (parameter.Attributes.HasFlag(ParameterAttributes.Out))
            p_mod = "out " + p_mod;

        string p_value = parameter.HasDefaultValue && parameter.Attributes.HasFlag(ParameterAttributes.Optional)
                       ? " = " + GetValueRepresentation(parameter.RawDefaultValue, parameter.ParameterType) : "";

        return $"{p_attrs.Select(p => p + ' ').StringConcat()}{p_mod}{p_type} {p_name}{p_value}";
    }

    protected (string arguments, string constraints) GetGenericArguments(IEnumerable<Type> generic_arguments)
    {
        List<string> genparams = new();
        string genconstr = "";

        foreach (Type gen in generic_arguments)
        {
            string genparam = GetTypeName(gen);
            List<string> constraints = new();

            if (gen.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                constraints.Add("class");
            if (gen.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                constraints.Add("struct");

            genparams.Add((gen.GenericParameterAttributes & GenericParameterAttributes.VarianceMask) switch
            {
                GenericParameterAttributes.Covariant => "out ",
                GenericParameterAttributes.Contravariant => "in ",
                _ => "",
            } + genparam);
            constraints.AddRange(gen.GetGenericParameterConstraints().Select(GetTypeName));

            if (gen.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
                constraints.Add("new()");

            if (constraints.Count > 0)
                genconstr += $" where {genparam} : {constraints.StringJoin(", ")}";
        }

        return (genparams.Count > 0 ? $"<{genparams.StringJoin(", ")}>" : "", genconstr);
    }

    protected (List<string> attributes, string signature) GenerateSignature(MethodBase method)
    {
        IEnumerable<CustomAttributeData> custom_attributes = method.CustomAttributes;
        List<string> parameterlist = GetParameters(method.GetParameters(), custom_attributes.Any(a => a.AttributeType == typeof(ExtensionAttribute)));
        List<string> attributes = GetAttributes(custom_attributes.Where(a => a.AttributeType != typeof(ExtensionAttribute)));
        string parameters = $"({parameterlist.StringJoin(", ")})";
        string signature;


        var x = method.CallingConvention;
        var y = method.MethodImplementationFlags;
        ////////////////////////////// TODO //////////////////////////////


        if (method is ConstructorInfo constructor)
        {
            string name = GetConstructorTypeName(constructor);

            signature = name + parameters;
        }
        else if (method is MethodInfo function)
        {
            (string genparams, string genconstr) = GetGenericArguments(function.GetGenericArguments());
            string name = function.Name;

            IEnumerable<CustomAttributeData> retattrs = (function.ReturnTypeCustomAttributes as ParameterInfo)?.CustomAttributes ?? Enumerable.Empty<CustomAttributeData>();
            (retattrs, string rettype) = ProcessNullabilityInfo(retattrs, function.ReturnType);

            attributes.AddRange(GetAttributes(retattrs, true));
            signature = $"{rettype} {name}{genparams}{parameters}{genconstr}";
        }
        else
            throw new NotImplementedException();

        return (attributes, signature);
    }

    public string GenerateSignature(FieldInfo field)
    {
        string? value = field.IsLiteral ? $" = {GetValueRepresentation(field.GetRawConstantValue(), field.FieldType)}" : null;



        throw new NotImplementedException();
    }

    public override string GenerateSignature(MemberInfo member)
    {
        IEnumerable<CustomAttributeData> custom_attributes = member.CustomAttributes;
        List<string> attributes;
        Type? container = member.DeclaringType;
        string modifiers;
        string signature;

        switch (member)
        {
            case FieldInfo field:
                (custom_attributes, string field_type) = ProcessNullabilityInfo(custom_attributes, field.FieldType);
                attributes = GetAttributes(custom_attributes);
                modifiers = GetModifiers(field.Attributes);
                signature = GenerateSignature(field);

                break;
            case MethodBase method:
                modifiers = GetModifiers(method.Attributes);

                if (container != (method as MethodInfo)?.GetBaseDefinition()?.DeclaringType)
                    modifiers += " override";

                (attributes, signature) = GenerateSignature(method);

                break;
            case EventInfo ei:
                var add = ei.AddMethod;
                var rem = ei.RemoveMethod;

                throw new NotImplementedException();
            case PropertyInfo pi:
                var set = pi.SetMethod;
                var get = pi.GetMethod;

                throw new NotImplementedException();
            case Type ti:
                throw new NotImplementedException();
            default:
                throw new NotImplementedException();
        }

        string indent = new(' ', Options.IndentationLevel * 4);

        return attributes.Append($"{modifiers} {signature};")
                         .Select(l => indent + l)
                         .StringJoin(Options.Compact ? "" : Environment.NewLine);
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

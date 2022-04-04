using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Unknown6656.Generics;

namespace Unknown6656.Runtime;


public abstract class SignatureProvider
{
    public SignatureOptions Options { get; init; } = SignatureOptions.Default;


    public abstract string GenerateSignature(MemberInfo member);
}

public class CSharpSignatureProvider
    : SignatureProvider
{
    private static string GetVisibilityModifiers(MethodAttributes attributes) => (from p in new[]
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

    private static string GetVisibilityModifiers(FieldAttributes attributes) => (from p in new[]
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

    private string GetName(Type type) => (Options.FullyQualifiedTypeNames ? type.FullName : null) ?? type.Name;

    private string GetRepresentation(object? value, Type type)
    {
        value switch
        {

        }
    }

    private List<string> GetAttributes(MemberInfo member)
    {
        List<string> attrs = new();

        foreach (var attr in member.CustomAttributes)
        {
            string name = GetName(attr.AttributeType);
            List<string> args = new();

            if (name.EndsWith("Attribute"))
                name = name[..^"Attribute".Length];

            foreach (CustomAttributeTypedArgument arg in attr.ConstructorArguments)
                args.Add(GetRepresentation(arg.Value, arg.ArgumentType));

            foreach (CustomAttributeNamedArgument arg in attr.NamedArguments)
                args.Add($"{arg.MemberName} = {GetRepresentation(arg.TypedValue.Value, arg.TypedValue.ArgumentType)}");

            attrs.Add($"[{name}{}]");
        }
    }


    public string GenerateCSharpSignature(MethodBase method, SignatureOptions options)
    {
        string modifiers = GetVisibilityModifiers(method.Attributes);
        Type? container = method.DeclaringType;

        if (container != (method as MethodInfo)?.GetBaseDefinition()?.DeclaringType)
            modifiers += " override";


    }

    public string GenerateCSharpSignature(FieldInfo field, SignatureOptions options)
    {
        string modifiers = field.Attributes.GetVisibilityModifiers();

    }



    public string GenerateCSharpSignature(MethodInfo method) => GenerateCSharpSignature(method, SignatureOptions.Default)

    public override string GenerateSignature(MemberInfo member)
    {
        string typename(Type type) => (options.FullyQualifiedTypeNames ? type.FullName : null) ?? type.Name;



        string modifiers;


        switch (member)
        {
            case FieldInfo fi:
                modifiers = GetVisibilityModifiers(fi.Attributes);
                break;
            case MethodBase mi:
                modifiers = GetVisibilityModifiers(mi.Attributes);
                break;
            case EventInfo ei:
                var add = ei.AddMethod;
                var rem = ei.RemoveMethod;



                break;
            case PropertyInfo pi:
                var 

                break;
            case Type ti:

                break;
        }









            UnmanagedExport

            CheckAccessOnOverride
            HasSecurity
            RequireSecObject





        method.Attributes;
        method.ReturnType;




    }
}

public record SignatureOptions
{
    public static SignatureOptions Default { get; } = new();

    public int IndentationLevel { get; init; } = 0;
    public bool MultiLine { get; init; } = true;
    public bool AppendSemicolon { get; init; } = false;
    public bool FullyQualifiedTypeNames { get; init; } = true;
}

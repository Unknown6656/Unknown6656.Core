using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System;

using Unknown6656.Generics;
using Unknown6656.Common;

namespace Unknown6656.Runtime;


public abstract unsafe class Disassembler<Config>
{
    public string Disassemble(byte[] bytes, Config config) => DisassembleIntoLines(bytes, config).StringJoinLines();

    public string Disassemble(byte* ptr, int length, Config config) => DisassembleIntoLines(ptr, length, config).StringJoinLines();

    public List<string> DisassembleIntoLines(byte[] bytes, Config config)
    {
        fixed (byte* ptr = bytes)
            return DisassembleIntoLines(ptr, bytes.Length, config);
    }

    public abstract List<string> DisassembleIntoLines(byte* ptr, int length, Config config);
}

public abstract unsafe class Disassembler
    : Disassembler<__empty>
{
    public string Disassemble(byte[] bytes) => DisassembleIntoLines(bytes).StringJoinLines();

    public string Disassemble(byte* ptr, int length) => DisassembleIntoLines(ptr, length).StringJoinLines();

    public List<string> DisassembleIntoLines(byte[] bytes)
    {
        fixed (byte* ptr = bytes)
            return DisassembleIntoLines(ptr, bytes.Length);
    }

    public List<string> DisassembleIntoLines(byte* ptr, int length) => DisassembleIntoLines(ptr, length, default);
}

public unsafe class ILDisassembler
    : Disassembler<Module>
{
    private static readonly ILSignatureProvider _ilsigprov = new();


    public string Disassemble(MethodInfo method) =>
        method.GetMethodBody() is { } body ? Disassemble(body, method.Module) : throw new ArgumentException("The given method does not define a method body.", nameof(method));

    public string Disassemble(MethodBody method, Module module)
    {
        byte[]? signature = LINQ.TryDo(() => module.ResolveSignature(method.LocalSignatureMetadataToken), null);
        IList<LocalVariableInfo> variables = method.LocalVariables;
        IList<ExceptionHandlingClause> handlers = method.ExceptionHandlingClauses;
        StringBuilder il = new();

        // TODO : do something with 'signature'

        il.AppendLine("{");
        il.AppendLine($"    .maxstack {method.MaxStackSize}");

        if (variables.Count is int l and > 0)
        {
            il.AppendLine($"    .locals {(method.InitLocals ? "init" : "")} (");

            for (int i = 0; i < l; ++i)
                il.AppendLine($"        [{variables[i].LocalIndex}] {variables[i].LocalType} {(variables[i].IsPinned ? "pinned" : "")}{(i < l - 1 ? "," : "")}");

            il.AppendLine($"    )");
        }

        il.AppendLines(Disassemble(method?.GetILAsByteArray(), module));
        il.AppendLine("}");

        return il.ToString();
    }

    public override List<string> DisassembleIntoLines(byte* start, int length, Module module)
    {
        List<string> lines = [];
        byte* ptr = start;
        byte* end = start + length;
        int processed = 0;

        while (ptr < end)
            try
            {
                if (Process(ptr, module, processed) is (int adv, string instr))
                {
                    lines.Add($"IL_{processed:x4}: {instr}");
                    ++processed;
                    ptr += adv;
                }
                else
                    break;
            }
            catch
            {
                lines.Add("<error while processing bytes>");

                break;
            }

        return lines;
    }

    private static unsafe (int adv, string instruction)? Process(byte* ptr, Module module, int line)
    {
        T get<T>(int offset = 1) where T : unmanaged => *(T*)(ptr + 1);
        string resolve_target(int target) => $"IL_{target + line:x4}";
        string resolve_type(int token) => _ilsigprov.GetTypeName(module.ResolveType(token));
        string resolve_field(int token) => _ilsigprov.GetCallsiteSignature(module.ResolveField(token));
        string resolve_method(int token) => _ilsigprov.GetCallsiteSignature(module.ResolveMethod(token));

        return *ptr switch
        {
            0x00 => (1, "nop"),
            0x01 => (1, "break"),
            0x02 => (1, "ldarg.0"),
            0x03 => (1, "ldarg.1"),
            0x04 => (1, "ldarg.2"),
            0x05 => (1, "ldarg.3"),
            0x06 => (1, "ldloc.0"),
            0x07 => (1, "ldloc.1"),
            0x08 => (1, "ldloc.2"),
            0x09 => (1, "ldloc.3"),
            0x0a => (1, "stloc.0"),
            0x0b => (1, "stloc.1"),
            0x0c => (1, "stloc.2"),
            0x0d => (1, "stloc.3"),
            0x0e => (2, $"ldarg.s {ptr[1]}"),
            0x0f => (2, $"ldarga.s {ptr[1]}"),
            0x10 => (2, $"starg.s {ptr[1]}"),
            0x11 => (2, $"ldloc.s {ptr[1]}"),
            0x12 => (2, $"ldloca.s {ptr[1]}"),
            0x13 => (2, $"stloc.s {ptr[1]}"),
            0x14 => (1, "ldnull"),
            0x15 => (1, "ldc.i4.m1"),
            0x16 => (1, "ldc.i4.0"),
            0x17 => (1, "ldc.i4.1"),
            0x18 => (1, "ldc.i4.2"),
            0x19 => (1, "ldc.i4.3"),
            0x1a => (1, "ldc.i4.4"),
            0x1b => (1, "ldc.i4.5"),
            0x1c => (1, "ldc.i4.6"),
            0x1d => (1, "ldc.i4.7"),
            0x1e => (1, "ldc.i4.8"),
            0x1f => (2, $"ldc.i4.s {ptr[1]}"),
            0x20 => (5, $"ldc.i4 0x{get<int>():x8}"),
            0x21 => (9, $"ldc.i8 0x{get<long>():x16}"),
            0x22 => (5, $"ldc.r4 {get<float>()}"),
            0x23 => (9, $"ldc.r8 {get<double>()}"),
            0x25 => (1, "dup"),
            0x26 => (1, "pop"),
            0x27 => (5, $"jmp {resolve_method(get<int>())}"),
            0x28 => (5, $"call {resolve_method(get<int>())}"),
            0x29 => (5, $"calli {resolve_method(get<int>())}"),
            0x2a => (1, "ret"),
            0x2b => (2, $"br.s {resolve_target(get<sbyte>())}"),
            0x2c => (2, $"brfalse.s {resolve_target(get<sbyte>())}"),
            0x2d => (2, $"brtrue.s {resolve_target(get<sbyte>())}"),
            0x2e => (2, $"beq.s {resolve_target(get<sbyte>())}"),
            0x2f => (2, $"bge.s {resolve_target(get<sbyte>())}"),
            0x30 => (2, $"bgt.s {resolve_target(get<sbyte>())}"),
            0x31 => (2, $"ble.s {resolve_target(get<sbyte>())}"),
            0x32 => (2, $"blt.s {resolve_target(get<sbyte>())}"),
            0x33 => (2, $"bne.un.s {resolve_target(get<sbyte>())}"),
            0x34 => (2, $"bge.un.s {resolve_target(get<sbyte>())}"),
            0x35 => (2, $"bgt.un.s {resolve_target(get<sbyte>())}"),
            0x36 => (2, $"ble.un.s {resolve_target(get<sbyte>())}"),
            0x37 => (2, $"blt.un.s {resolve_target(get<sbyte>())}"),
            0x38 => (5, $"br {resolve_target(get<int>())}"),
            0x39 => (5, $"brfalse {resolve_target(get<int>())}"),
            0x3a => (5, $"brtrue {resolve_target(get<int>())}"),
            0x3b => (5, $"beq {resolve_target(get<int>())}"),
            0x3c => (5, $"bge {resolve_target(get<int>())}"),
            0x3d => (5, $"bgt {resolve_target(get<int>())}"),
            0x3e => (5, $"ble {resolve_target(get<int>())}"),
            0x3f => (5, $"blt {resolve_target(get<int>())}"),
            0x40 => (5, $"beq.un {resolve_target(get<int>())}"),
            0x41 => (5, $"bge.un {resolve_target(get<int>())}"),
            0x42 => (5, $"bgt.un {resolve_target(get<int>())}"),
            0x43 => (5, $"ble.un {resolve_target(get<int>())}"),
            0x44 => (5, $"blt.un {resolve_target(get<int>())}"),
            0x45 => LINQ.Do(delegate
            {
                uint n = get<uint>();
                string[] offs = new string[n];

                for (int i = 0; i < offs.Length; ++i)
                    offs[i] = resolve_target(get<int>(1 + sizeof(uint) + i * sizeof(int)));

                return (1 + sizeof(uint) * offs.Length * sizeof(int), $"switch {n} {offs.StringJoin(" ")}");
            }),
            0x46 => (1, "ldind.i1"),
            0x47 => (1, "ldind.u1"),
            0x48 => (1, "ldind.i2"),
            0x49 => (1, "ldind.u2"),
            0x4a => (1, "ldind.i4"),
            0x4b => (1, "ldind.u4"),
            0x4c => (1, "ldind.i8"),
            0x4d => (1, "ldind.i"),
            0x4e => (1, "ldind.r4"),
            0x4f => (1, "ldind.r8"),
            0x50 => (1, "ldind.ref"),
            0x51 => (1, "stind.r4"),
            0x52 => (1, "stind.i1"),
            0x53 => (1, "stind.i2"),
            0x54 => (1, "stind.i4"),
            0x55 => (1, "stind.i8"),
            0x56 => (1, "stind.r4"),
            0x57 => (1, "stind.r8"),
            0x58 => (1, "add"),
            0x59 => (1, "sub"),
            0x5a => (1, "mul"),
            0x5b => (1, "div"),
            0x5c => (1, "div.un"),
            0x5d => (1, "rem"),
            0x5e => (1, "rem.un"),
            0x5f => (1, "and"),
            0x60 => (1, "or"),
            0x61 => (1, "xor"),
            0x62 => (1, "shl"),
            0x63 => (1, "shr"),
            0x64 => (1, "shr.un"),
            0x65 => (1, "neg"),
            0x66 => (1, "not"),
            0x67 => (1, "conv.i1"),
            0x68 => (1, "conv.i2"),
            0x69 => (1, "conv.i4"),
            0x6a => (1, "conv.i8"),
            0x6b => (1, "conv.r4"),
            0x6c => (1, "conv.r8"),
            0x6d => (1, "conv.u4"),
            0x6e => (1, "conv.u8"),
            0x6f => (5, $"callvirt {resolve_method(get<int>())}"),
            0x70 => (5, $"cpobj {resolve_type(get<int>())}"),
            0x71 => (5, $"ldobj {resolve_type(get<int>())}"),
            0x72 => (5, $"ldstr {module.ResolveString(get<int>()).ToCSharpEscaped(true)}"),
            0x73 => (5, $"newobj {resolve_method(get<int>())}"),
            0x74 => (5, $"castclass {resolve_type(get<int>())}"),
            0x75 => (5, $"isinst {resolve_type(get<int>())}"),
            0x76 => (1, "conv.r.un"),
            0x79 => (5, $"unbox {resolve_type(get<int>())}"),
            0x7a => (1, "throw"),
            0x7b => (5, $"ldfld {resolve_field(get<int>())}"),
            0x7c => (5, $"ldflda {resolve_field(get<int>())}"),
            0x7d => (5, $"stfld {resolve_field(get<int>())}"),
            0x7e => (5, $"ldsfld {resolve_field(get<int>())}"),
            0x7f => (5, $"ldsflda {resolve_field(get<int>())}"),
            0x80 => (5, $"stsfld {resolve_field(get<int>())}"),
            0x81 => (5, $"stobj {resolve_type(get<int>())}"),
            0x82 => (1, "conv.ovf.i1.un"),
            0x83 => (1, "conv.ovf.i2.un"),
            0x84 => (1, "conv.ovf.i4.un"),
            0x85 => (1, "conv.ovf.i8.un"),
            0x86 => (1, "conv.ovf.u1.un"),
            0x87 => (1, "conv.ovf.u2.un"),
            0x88 => (1, "conv.ovf.u4.un"),
            0x89 => (1, "conv.ovf.u8.un"),
            0x8a => (1, "conv.ovf.i.un"),
            0x8b => (1, "conv.ovf.u.un"),
            0x8c => (5, $"box {resolve_type(get<int>())}"),
            0x8d => (5, $"newarr {resolve_type(get<int>())}"),
            0x8e => (1, "ldlen"),
            0x8f => (5, $"ldelema {resolve_type(get<int>())}"),
            0x90 => (1, "ldelem.i1"),
            0x91 => (1, "ldelem.u1"),
            0x92 => (1, "ldelem.i2"),
            0x93 => (1, "ldelem.u2"),
            0x94 => (1, "ldelem.i4"),
            0x95 => (1, "ldelem.u4"),
            0x96 => (1, "ldelem.i8"),
            0x97 => (1, "ldelem.i"),
            0x98 => (1, "ldelem.r4"),
            0x99 => (1, "ldelem.r8"),
            0x9a => (1, "ldelem.ref"),
            0x9b => (1, "stelem.i"),
            0x9c => (1, "stelem.i1"),
            0x9d => (1, "stelem.i2"),
            0x9e => (1, "stelem.i4"),
            0x9f => (1, "stelem.i8"),
            0xa0 => (1, "stelem.r4"),
            0xa1 => (1, "stelem.r8"),
            0xa2 => (1, "stelem.ref"),
            0xa3 => (5, $"ldelem {resolve_type(get<int>())}"),
            0xa4 => (5, $"stelem {resolve_type(get<int>())}"),
            0xa5 => (5, $"unbox.any {resolve_type(get<int>())}"),
            0xb3 => (1, "conv.ovf.i1"),
            0xb4 => (1, "conv.ovf.u1"),
            0xb5 => (1, "conv.ovf.i2"),
            0xb6 => (1, "conv.ovf.u2"),
            0xb7 => (1, "conv.ovf.i4"),
            0xb8 => (1, "conv.ovf.u4"),
            0xb9 => (1, "conv.ovf.i8"),
            0xba => (1, "conv.ovf.u8"),
            0xc2 => (5, $"refanyval {resolve_type(get<int>())}"),
            0xc3 => (1, "ckfinite"),
            0xc6 => (5, $"mkrefany {resolve_type(get<int>())}"),
            0xd0 => (5, $"ldtoken 0x{get<int>():x8}"),
            0xd1 => (1, "conv.u2"),
            0xd2 => (1, "conv.u1"),
            0xd3 => (1, "conv.i"),
            0xd4 => (1, "conv.ovf.i"),
            0xd5 => (1, "conv.ovf.u"),
            0xd6 => (1, "add.ovf"),
            0xd7 => (1, "add.ovf.un"),
            0xd8 => (1, "mul.ovf"),
            0xd9 => (1, "mul.ovf.un"),
            0xda => (1, "sub.ovf"),
            0xdb => (1, "sub.ovf.un"),
            0xdc => (1, "endfinally"),
            0xdd => (5, $"leave {resolve_target(get<int>())}"),
            0xde => (2, $"leave.s {resolve_target(get<sbyte>())}"),
            0xdf => (1, "stind.i"),
            0xe0 => (1, "conv.u"),
            0xfe when ptr[1] is 0x00 => (1, "arglist"),
            0xfe when ptr[1] is 0x01 => (1, "ceq"),
            0xfe when ptr[1] is 0x02 => (1, "cgt"),
            0xfe when ptr[1] is 0x03 => (1, "cgt.un"),
            0xfe when ptr[1] is 0x04 => (1, "clt"),
            0xfe when ptr[1] is 0x05 => (1, "clt.un"),
            0xfe when ptr[1] is 0x06 => (5, $"ldftn {resolve_method(get<int>())}"),
            0xfe when ptr[1] is 0x07 => (5, $"ldvirtftn {resolve_method(get<int>())}"),
            0xfe when ptr[1] is 0x09 => (3, $"ldarg {get<ushort>()}"),
            0xfe when ptr[1] is 0x0a => (3, $"ldarga {get<ushort>()}"),
            0xfe when ptr[1] is 0x0b => (3, $"starg {get<ushort>()}"),
            0xfe when ptr[1] is 0x0c => (3, $"ldloc {get<ushort>()}"),
            0xfe when ptr[1] is 0x0d => (3, $"ldloca {get<ushort>()}"),
            0xfe when ptr[1] is 0x0e => (3, $"stloc {get<ushort>()}"),
            0xfe when ptr[1] is 0x0f => (1, "localloc"),
            0xfe when ptr[1] is 0x11 => (1, "endfilter"),
            0xfe when ptr[1] is 0x12 => (2, $"unaligned. {ptr[1]}"),
            0xfe when ptr[1] is 0x13 => (1, "volatile."),
            0xfe when ptr[1] is 0x14 => (1, "tail."),
            0xfe when ptr[1] is 0x15 => (5, $"initobj {resolve_type(get<int>())}"),
            0xfe when ptr[1] is 0x16 => (5, $"constrained. {resolve_type(get<int>())}"),
            0xfe when ptr[1] is 0x17 => (1, "cpblk"),
            0xfe when ptr[1] is 0x18 => (1, "initblk"),
            0xfe when ptr[1] is 0x19 => (2, $"no.{((ptr[1] & 1) != 0 ? " typecheck" : "") +
                                                  ((ptr[1] & 2) != 0 ? " rangecheck" : "") +
                                                  ((ptr[1] & 4) != 0 ? " nullcheck" : "")}"),
            0xfe when ptr[1] is 0x1a => (1, "rethrow"),
            0xfe when ptr[1] is 0x1c => (5, $"sizeof {resolve_type(get<int>())}"),
            0xfe when ptr[1] is 0x1d => (1, "refanytype"),
            0xfe when ptr[1] is 0x1e => (1, "readonly."),

            0x24 or
            0x77 or
            0x78 or
            (>= 0xa6 and <= 0xb2) or
            (>= 0xbb and <= 0xc1) or
            0xc4 or
            0xc5 or
            (>= 0xc7 and <= 0xcf) or
            (>= 0xe1 and <= 0xef) or
            (>= 0xf0 and <= 0xfd) or
            0xfe or
            0xff or
            _ => (1, $"<undefined opcode 0x{*ptr:x2}>"),
        };
    }
}
